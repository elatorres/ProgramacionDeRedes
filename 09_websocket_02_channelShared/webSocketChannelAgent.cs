using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Common;

public class WebSocketChannelAgent : IDisposable
{
    private readonly WebSocketChannel _channel;
    private readonly ConcurrentQueue<IncomingMessage> _messageQueue;
    private Task _processingTask;
    private bool _isDisposed;
    private readonly object _lock = new object();

    public int Id { get; }
    public string Description { get; }
    public bool IsConnected { get; private set; }

    public event EventHandler<MessageReceivedEventArgs> MessageReceived;
    public event EventHandler Disconnected;
    public event EventHandler<Exception> ErrorOccurred;

    public WebSocketChannelAgent(WebSocketChannel channel, int id, string description)
    {
        _channel = channel ?? throw new ArgumentNullException(nameof(channel));
        Id = id;
        Description = description ?? "Unnamed Agent";
        _messageQueue = new ConcurrentQueue<IncomingMessage>();
        IsConnected = true;
        _processingTask = Task.Run(ProcessMessagesAsync);
    }

    public async Task SendMessageAsync(int destinationId, string message)
    {
        if (!IsConnected)
            throw new InvalidOperationException("Agent is not connected");

        try
        {
            await _channel.SendMessageAsync(Id, destinationId, message);
        }
        catch (Exception ex)
        {
            IsConnected = false;
            ErrorOccurred?.Invoke(this, ex);
            OnDisconnected();
            throw;
        }
    }

    internal async Task ProcessMessageAsync(int sourceId, string message)
    {
        if (_isDisposed || !IsConnected)
            return;

        _messageQueue.Enqueue(new IncomingMessage(sourceId, message));
        await Task.CompletedTask;
    }

    private async Task ProcessMessagesAsync()
    {
        while (!_isDisposed && IsConnected)
        {
            if (_messageQueue.TryDequeue(out var message))
            {
                try
                {
                    MessageReceived?.Invoke(this, new MessageReceivedEventArgs(message.SourceId, message.Message));
                }
                catch (Exception ex)
                {
                    ErrorOccurred?.Invoke(this, ex);
                }
            }
            else
            {
                await Task.Delay(10);
            }
        }
    }

    protected virtual void OnDisconnected()
    {
        IsConnected = false;
        Disconnected?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        if (_isDisposed) return;

        _isDisposed = true;
        IsConnected = false;
        OnDisconnected();
        _messageQueue.Clear();
        _processingTask?.Wait(1000);
    }

    private class IncomingMessage
    {
        public int SourceId { get; }
        public string Message { get; }

        public IncomingMessage(int sourceId, string message)
        {
            SourceId = sourceId;
            Message = message;
        }
    }
}

public class MessageReceivedEventArgs : EventArgs
{
    public int SourceId { get; }
    public string Message { get; }

    public MessageReceivedEventArgs(int sourceId, string message)
    {
        SourceId = sourceId;
        Message = message;
    }
}