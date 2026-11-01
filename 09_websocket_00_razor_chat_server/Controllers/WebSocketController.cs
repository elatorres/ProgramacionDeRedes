using System.Net.WebSockets;
using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace WebApplication1.Controllers;

public class WebSocketController : ControllerBase
{
    static List<WebSocket> _webSockets = new List<WebSocket>();
    static Task? _sendingTask;
    [Route("/ws")]
    public async Task Get()
    {
        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            WebSocket webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            _webSockets.Add(webSocket);
            await HandleConnection(webSocket);
        }
        else
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
    }
    
    private static async Task HandleConnection(WebSocket webSocket)
    {
        var receiveTask = ReceiveMessageAsync(webSocket);
        if(_sendingTask == null || _sendingTask.IsCompleted)
            _sendingTask = SendMessageTaskAsync();

        await receiveTask;
    }
    
    private static async Task ReceiveMessageAsync(WebSocket webSocket)
    {
        var bufferArr = new byte[1024 * 4];
        var receiveResult = await webSocket.ReceiveAsync(
            new ArraySegment<byte>(bufferArr), CancellationToken.None);
        

        while (!receiveResult.CloseStatus.HasValue)
        {
            string message = Encoding.UTF8.GetString(bufferArr, 0, receiveResult.Count);
            Console.WriteLine($"Recibido: {message}");
            
            receiveResult = await webSocket.ReceiveAsync(
                new ArraySegment<byte>(bufferArr), CancellationToken.None);
        }

        if (webSocket.State == WebSocketState.Open)
            await webSocket.CloseAsync(
                receiveResult.CloseStatus.Value,
                receiveResult.CloseStatusDescription,
                CancellationToken.None);
    }
    
    private static async Task SendMessageTaskAsync()
    {
        while (_webSockets.Count > 0)
        {
            var message = Console.ReadLine() ?? "Mensaje desde el servidor";
           
            foreach (var webSocket in _webSockets)
            {
                await SendMessageAsync(webSocket, message);
            }
        }
    }

    private static async Task SendMessageAsync(WebSocket webSocket, string message)
    {
        if(webSocket.State != WebSocketState.Open) return;
        #region Opcional para cerrar conexion desde el lado del servidor
        if(message.ToLower() == "exit")
        {
            await webSocket.SendAsync(
                new ArraySegment<byte>(Encoding.UTF8.GetBytes("Servidor cerrando conexion...")),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None);
            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Servidor cerrando conexion", CancellationToken.None);
            return;
        }
        #endregion
        byte[] messageBuffer = Encoding.UTF8.GetBytes(message);
        var messageSegment = new ArraySegment<byte>(messageBuffer);
        await webSocket.SendAsync(
            messageSegment, 
            WebSocketMessageType.Text, 
            true, 
            CancellationToken.None 
        );
    }
}