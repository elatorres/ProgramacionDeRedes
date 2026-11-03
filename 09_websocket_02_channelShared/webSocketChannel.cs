using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Common;

public class WebSocketChannel : IDisposable
{
    private readonly ClientWebSocket _clientWebSocket;
    private readonly WebSocket _webSocket;
    private readonly bool _isServerSide;
    private readonly CancellationTokenSource _cts;
    private readonly ConcurrentDictionary<int, WebSocketChannelAgent> _agents;
    private readonly ConcurrentQueue<WebSocketMessage> _messageQueue;
    private int _nextAgentId = 1;
    private bool _isDisposed;
    private readonly object _lock = new object();
    private Task _receiveTask;
    private Task _sendTask;

    public event EventHandler<AgentEventArgs> AgentRegistered;
    public event EventHandler<AgentEventArgs> AgentUnregistered;

    // Client-side constructor
    public WebSocketChannel(string uri, int port) : this(new ClientWebSocket(), false)
    {
        _clientWebSocket = new ClientWebSocket();
        _webSocket = _clientWebSocket;
        _isServerSide = false;
        _uri = new Uri($"{uri}:{port}");
    }

    // Server-side constructor
    private WebSocketChannel(WebSocket webSocket, bool isServerSide)
    {
        _webSocket = webSocket ?? throw new ArgumentNullException(nameof(webSocket));
        _clientWebSocket = null;
        _isServerSide = isServerSide;
        _cts = new CancellationTokenSource();
        _agents = new ConcurrentDictionary<int, WebSocketChannelAgent>();
        _messageQueue = new ConcurrentQueue<WebSocketMessage>();
    }

    // Factory method for server-side creation
    public static WebSocketChannel CreateFromWebSocket(WebSocket webSocket)
    {
        return new WebSocketChannel(webSocket, true);
    }

    public async Task ConnectAsync()
    {
        if (_isServerSide)
            throw new InvalidOperationException("Server-side channels are already connected");

        try
        {
            await _clientWebSocket.ConnectAsync(_uri, _cts.Token);
            StartReceiveAndSendTasks();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to connect to {_uri}", ex);
        }
    }

    public void StartServerChannel()
    {
        if (!_isServerSide)
            throw new InvalidOperationException("This method is only for server-side channels");

        StartReceiveAndSendTasks();
    }

    private void StartReceiveAndSendTasks()
    {
        _receiveTask = Task.Run(ReceiveMessagesAsync);
        _sendTask = Task.Run(SendMessagesAsync);
    }

    public WebSocketChannelAgent RegisterAgent(string description)
    {
        lock (_lock)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(WebSocketChannel));

            var agent = new WebSocketChannelAgent(this, _nextAgentId++, description);
            _agents.TryAdd(agent.Id, agent);
            agent.Disconnected += OnAgentDisconnected;

            AgentRegistered?.Invoke(this, new AgentEventArgs(agent));
            return agent;
        }
    }

    internal void UnregisterAgent(int agentId)
    {
        if (_agents.TryRemove(agentId, out var agent))
        {
            agent.Disconnected -= OnAgentDisconnected;
            AgentUnregistered?.Invoke(this, new AgentEventArgs(agent));
        }
    }

    internal async Task SendMessageAsync(int sourceId, int destinationId, string message)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(WebSocketChannel));

        if (_webSocket.State != WebSocketState.Open)
            throw new InvalidOperationException("WebSocket is not connected");

        var jsonMessage = new MessagePayload
        {
            Source = sourceId,
            Destination = destinationId,
            Message = message,
            Timestamp = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(jsonMessage);
        var bytes = Encoding.UTF8.GetBytes(json);
        var segment = new ArraySegment<byte>(bytes);

        _messageQueue.Enqueue(new WebSocketMessage(segment, WebSocketMessageType.Text));
    }

    private async Task SendMessagesAsync()
    {
        while (!_cts.Token.IsCancellationRequested && _webSocket.State == WebSocketState.Open)
        {
            if (_messageQueue.TryDequeue(out var message))
            {
                try
                {
                    await _webSocket.SendAsync(message.Buffer, message.Type, true, _cts.Token);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Send error: {ex.Message}");
                    break;
                }
            }
            else
            {
                await Task.Delay(10, _cts.Token);
            }
        }
    }

    private async Task ReceiveMessagesAsync()
    {
        var buffer = new byte[4096];

        while (!_cts.Token.IsCancellationRequested && _webSocket.State == WebSocketState.Open)
        {
            try
            {
                var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    await ProcessReceivedMessageAsync(message);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Receive error: {ex.Message}");
                break;
            }
        }

        // Clean up when receive loop exits
        await CloseAsync();
    }

    private async Task ProcessReceivedMessageAsync(string jsonMessage)
    {
        try
        {
            var payload = JsonSerializer.Deserialize<MessagePayload>(jsonMessage);

            if (payload == null)
                return;

            // Handle messages for agent 0 (channel itself)
            if (payload.Destination == 0)
            {
                await ProcessSystemMessageAsync(payload);
                return;
            }

            // Route to destination agent
            if (_agents.TryGetValue(payload.Destination, out var agent))
            {
                await agent.ProcessMessageAsync(payload.Source, payload.Message);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Agent {payload.Destination} not found");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Message processing error: {ex.Message}");
        }
    }

    private async Task ProcessSystemMessageAsync(MessagePayload payload)
    {
        if (payload.Message?.ToLower() == "directory")
        {
            var directory = BuildDirectoryMessage();
            await SendMessageAsync(0, payload.Source, directory);
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"Unknown system message: {payload.Message}");
        }
    }

    private string BuildDirectoryMessage()
    {
        var agents = new List<AgentInfo>();
        foreach (var agent in _agents.Values)
        {
            agents.Add(new AgentInfo { Id = agent.Id, Description = agent.Description });
        }
        return JsonSerializer.Serialize(agents);
    }

    private async Task CloseAsync()
    {
        if (_webSocket.State == WebSocketState.Open)
        {
            try
            {
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
            }
            catch { }
        }

        _cts.Cancel();
        _webSocket.Dispose();
        Dispose();
    }

    private void OnAgentDisconnected(object sender, EventArgs e)
    {
        if (sender is WebSocketChannelAgent agent)
        {
            UnregisterAgent(agent.Id);
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;

        _isDisposed = true;
        _cts.Cancel();

        if (_clientWebSocket != null)
        {
            _clientWebSocket?.Dispose();
        }
        else
        {
            _webSocket?.Dispose();
        }
        
        _cts?.Dispose();

        foreach (var agent in _agents.Values)
        {
            agent.Dispose();
        }
        _agents.Clear();
    }

    private class WebSocketMessage
    {
        public ArraySegment<byte> Buffer { get; }
        public WebSocketMessageType Type { get; }

        public WebSocketMessage(ArraySegment<byte> buffer, WebSocketMessageType type)
        {
            Buffer = buffer;
            Type = type;
        }
    }

    private class MessagePayload
    {
        public int Source { get; set; }
        public int Destination { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
    }

    private class AgentInfo
    {
        public int Id { get; set; }
        public string Description { get; set; }
    }

    private Uri _uri;
}

public class AgentEventArgs : EventArgs
{
    public WebSocketChannelAgent Agent { get; }

    public AgentEventArgs(WebSocketChannelAgent agent)
    {
        Agent = agent;
    }
}
