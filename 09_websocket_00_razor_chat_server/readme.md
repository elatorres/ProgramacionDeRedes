## Variables y requisitos
El Target de .NET es 9.0 (Se puede usar para 8.0)

La url para conectarse es `ws://localhost:[PORT]/ws` el puerto se encuentra definido en el archivo `launchSettings.json`
## Objetivo
Crear un servidor WebSocket en .NET que permita la comunicacion bidireccional entre cliente y servidor. El servidor recibe los mensajes de los clientes y los muestra en la consola, ademas al enviar un mensaje desde la consola del servidor, este se envia a todos los clientes conectados.

## Pasos para creacion de un servidor WebSocket en .NET
1. Crear proyecto web/empty
2. Abrir `Program.cs`
3. Borrar `app.MapGet("/", () => "Hello World!");`
4. Agregar `builder.Services.AddControllers();`
5. Agregar `app.MapControllers();`
6. Agregar uso de WebSockets `app.UseWebSockets();`
7. Agregar configuracion de WebSocket
```csharp
WebSocketOptions wsOptions = new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(120),
};

app.UseWebSockets(wsOptions);
```
8. Crear carpeta `Controllers`
9. Crear clase `WebSocketController` en la carpeta `Controllers`
10. Agregar ruta para conectarse a WebSocket
```csharp
[Route("/ws")]
public async Task Get()
{
    if (HttpContext.WebSockets.IsWebSocketRequest)
    {
        WebSocket webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
        await HandleConnection(webSocket);
    }
    else
    {
        HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
    }
}
```
11. Agregar metodo `HandleConnection`
```csharp
private static async Task HandleConnection(WebSocket webSocket)
{
    var receiveTask = ReceiveMessage(webSocket);
    var sendTask = SendMessage(webSocket);

    await Task.WhenAll(receiveTask, sendTask);
}
```
12. Agregar metodo `ReceiveMessage`
```csharp
private static async Task ReceiveMessage(WebSocket webSocket)
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
```
13. Agregar metodo `SendMessage`
```csharp
private static async Task SendMessage(WebSocket webSocket)
{

    while (webSocket.State == WebSocketState.Open)
    {
        var message = Console.ReadLine() ?? "Mensaje desde el servidor";

        #region Opcional para cerrar conexion desde el lado del servidor
        if(message.ToLower() == "exit")
        {
            await webSocket.SendAsync(
                new ArraySegment<byte>(Encoding.UTF8.GetBytes("Servidor cerrando conexion...")),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None);
            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Servidor cerrando conexion", CancellationToken.None);
            break;
        }
        #endregion


        byte[] messageBuffer = Encoding.UTF8.GetBytes(message);
        var messageSegment = new ArraySegment<byte>(messageBuffer);

        await webSocket.SendAsync(
            messageSegment, // mensaje a enviar
            WebSocketMessageType.Text, // Tipo de mensaje (texto, binario, close)
            true, // Indica si es el final del mensaje. Si es false, se espera más partes
            CancellationToken.None 
        );
    }
}
```
14. Ejecutar el proyecto