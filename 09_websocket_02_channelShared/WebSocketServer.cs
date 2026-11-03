using System;
using System.Collections.Generic;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Common;

public class WebSocketServer : IDisposable
{
    private HttpListener _listener;
    private readonly CancellationTokenSource _cts;
    private readonly List<WebSocketChannel> _channels;
    private bool _isRunning;
    private Task _acceptTask;
    private readonly int _port;
    
    // el constructor de mi servidor, Recibe sólo el port.
    public WebSocketServer(int port)
    {
        _port = port;
        _listener = new HttpListener();
        // podría tener un uri http://localhost:{port}/ws pero no esta vez
        _listener.Prefixes.Add($"http://localhost:{port}/");
        _cts = new CancellationTokenSource();
        _channels = new List<WebSocketChannel>();
    }
    
    
    // Comienza a funcionar
    public async Task StartAsync()
    {
        _listener.Start();
        _isRunning = true;
        // Recibe y acepta conexiones
        _acceptTask = Task.Run(AcceptConnectionsAsync);
        Console.WriteLine($"WebSocket Server started on port {_port}");
    }

    private async Task AcceptConnectionsAsync()
    {
        // mientras no se cancele
        while (!_cts.Token.IsCancellationRequested && _isRunning)
        {
            try
            {
                // Vemos si es una solicitud de websocket
                var context = await _listener.GetContextAsync();
                if (context.Request.IsWebSocketRequest)
                {
                    _ = Task.Run(() => HandleWebSocketConnectionAsync(context));
                }
                else
                {
                    // sino es erroneo
                    context.Response.StatusCode = 400;
                    context.Response.Close();
                }
            }
            catch (Exception ex)
            {
                // Si no es cancellation
                if (!_cts.Token.IsCancellationRequested)
                    Console.WriteLine($"Accept error: {ex.Message}");
            }
        }
    }

    private async Task HandleWebSocketConnectionAsync(HttpListenerContext context)
    {
        try
        {
            // recibo el websocket
            var wsContext = await context.AcceptWebSocketAsync(null);
            var webSocket = wsContext.WebSocket;

            // Create channel from the accepted websocket using the factory method
            var channel = WebSocketChannel.CreateFromWebSocket(webSocket);
            channel.StartServerChannel();

            // Track channel
            lock (_channels)
            {
                _channels.Add(channel);
            }

            Console.WriteLine("New WebSocket connection established");

            // Create server tasks that will communicate through this channel
            // Luego se podría configurar diferentes tipos de tareas.
            await CreateServerTasksAsync(channel);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"WebSocket handling error: {ex.Message}");
        }
    }

    private async Task CreateServerTasksAsync(WebSocketChannel channel)
    {
        // Register different agents
        var agent1 = channel.RegisterAgent("Temperature Sensor");
        var agent2 = channel.RegisterAgent("Humidity Sensor");
        var agent3 = channel.RegisterAgent("Light Sensor");
        var agent4 = channel.RegisterAgent("System Monitor");

        // Set up event handlers for each agent
        SetupAgent(agent1, "Temperature", () => GetTemperature());
        SetupAgent(agent2, "Humidity", () => GetHumidity());
        SetupAgent(agent3, "Light", () => GetLightLevel());
        SetupAgent(agent4, "System", () => GetSystemStatus());

        // Start simulated communication
        _ = Task.Run(() => SimulateCommunicationAsync(agent1, agent2, agent3, agent4));
    }

    private void SetupAgent(WebSocketChannelAgent agent, string type, Func<string> dataGenerator)
    {
        agent.MessageReceived += async (sender, args) =>
        {
            try
            {
                var request = JsonSerializer.Deserialize<RequestMessage>(args.Message);
                if (request?.Command == "GET_DATA")
                {
                    var response = dataGenerator();
                    await agent.SendMessageAsync(args.SourceId, JsonSerializer.Serialize(new ResponseMessage
                    {
                        RequestId = request.RequestId,
                        Data = response,
                        Status = "SUCCESS"
                    }));
                }
                else
                {
                    Console.WriteLine($"Agent {agent.Id} ({agent.Description}) received: {args.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing message for agent {agent.Id}: {ex.Message}");
            }
        };

        agent.ErrorOccurred += (sender, ex) =>
        {
            Console.WriteLine($"Agent {agent.Id} error: {ex.Message}");
        };

        agent.Disconnected += (sender, args) =>
        {
            Console.WriteLine($"Agent {agent.Id} disconnected");
        };
    }

    private async Task SimulateCommunicationAsync(params WebSocketChannelAgent[] agents)
    {
        var random = new Random();
        while (!_cts.Token.IsCancellationRequested)
        {
            try
            {
                // Simulate messages between agents
                var source = agents[random.Next(agents.Length)];
                var dest = agents[random.Next(agents.Length)];

                if (source.Id != dest.Id && dest.IsConnected)
                {
                    var message = new
                    {
                        Type = "QUERY",
                        Action = "GET_STATUS",
                        Timestamp = DateTime.UtcNow
                    };

                    await source.SendMessageAsync(dest.Id, JsonSerializer.Serialize(message));
                    Console.WriteLine($"{source.Description} -> {dest.Description}: STATUS_REQUEST");
                }

                await Task.Delay(random.Next(3000, 8000));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Simulation error: {ex.Message}");
            }
        }
    }

    private string GetTemperature() => $"{new Random().Next(18, 28)}°C";
    private string GetHumidity() => $"{new Random().Next(30, 80)}%";
    private string GetLightLevel() => $"{new Random().Next(100, 1000)} lux";
    private string GetSystemStatus() => "OK - System running normally";

    public async Task StopAsync()
    {
        _isRunning = false;
        _cts.Cancel();
        _listener?.Stop();

        foreach (var channel in _channels)
        {
            channel?.Dispose();
        }
        _channels.Clear();

        if (_acceptTask != null)
            await _acceptTask;
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _listener?.Close();
        _cts?.Dispose();

        foreach (var channel in _channels)
        {
            channel?.Dispose();
        }
        _channels.Clear();
    }

    private class RequestMessage
    {
        public string RequestId { get; set; }
        public string Command { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
    }

    private class ResponseMessage
    {
        public string RequestId { get; set; }
        public string Data { get; set; }
        public string Status { get; set; }
    }
}

