using System.Net.WebSockets;

namespace ClientWS.Services;

public class WebSocketService : IAsyncDisposable
{
    private ClientWebSocket _webSocket = new();

    public async Task ConnectAsync(string uri)
    {
        if (_webSocket.State == WebSocketState.Open)
            return;
        
        await _webSocket.ConnectAsync(new Uri(uri), CancellationToken.None);
        Console.WriteLine("Conectado al servidor WebSocket.");
    }
    
    public async Task SendMessageAsync(string message)
    {
        if (_webSocket.State != WebSocketState.Open)
            throw new InvalidOperationException("La conexión WebSocket no está abierta.");
        
        var messageBytes = System.Text.Encoding.UTF8.GetBytes(message);
        await _webSocket.SendAsync(
            new ArraySegment<byte>(messageBytes),
            WebSocketMessageType.Text,
            true,
            CancellationToken.None);
        
        Console.WriteLine($"Enviado: {message}");
    }
    
    public async Task<string?> ReceiveMessageAsync()
    {
        if (_webSocket.State != WebSocketState.Open)
            throw new InvalidOperationException("La conexión WebSocket no está abierta.");
        
        var buffer = new byte[1024 * 4];
        var result = await _webSocket.ReceiveAsync(
            new ArraySegment<byte>(buffer), CancellationToken.None);
        
        if (result.MessageType == WebSocketMessageType.Close)
        {
            await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Cerrando", CancellationToken.None);
            return null;
        }
        
        var message = System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count);
        Console.WriteLine($"Recibido: {message}");
        return message;
    }
    
    public async ValueTask DisposeAsync()
    {
        if (_webSocket.State == WebSocketState.Open)
        {
            await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Cerrando", CancellationToken.None);
        }
        _webSocket.Dispose();
        _webSocket = null;
    }
    
    
}