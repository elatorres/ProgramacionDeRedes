using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

// Este es un servidor http que acepta conexiones GET y devuelve una cookie
// Siempre la misma
// Es para probar con los clientes que reciben cookies.

class SimpleHttpServer
{
    static async Task Main()
    {
        // Empezar a escuchar ...
        TcpListener listener = new TcpListener(IPAddress.Any, 8080);
        listener.Start();
        
        // Informamos sobre la operación
        Console.WriteLine("Server started on http://localhost:8080");
        Console.WriteLine("Type 'exit' + Enter to stop the server.");

        // One CTS for the whole server lifetime
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        // Console.ReadLine() no es cancelable y es bloqueante, por lo que quedaría
        // esperando allí. Esto se gestiona simplemente finalizando su ejecución al
        // cancelar el token. Luego de recibir el 'exit', la comprobación del
        // while(!token.IsCancellationRequested) en la siguiente iteración es lo que
        // detiene el proceso, o bien este termina de forma natural por el fin del hilo
        // Main o cuando se cierra stdin (devolviendo null).
        // Este es el patrón estándar y resulta suficiente en este caso, ya que el
        // proceso va a finalizar de todos modos; no es necesario forzar la terminación
        // ReadLine. 
        // Tarea de background: espera por el comando "exit"
        var consoleWatcher = Task.Run(() =>
        {
            while (!token.IsCancellationRequested)
            {
                var line = Console.ReadLine();
                if (line == null) return; // stdin closed
                if (line.Trim().Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    // SI es exit ...
                    Console.WriteLine("[Server] Shutdown requested from console...");
                    // ... Cancelamos
                    cts.Cancel();
                    return;
                }
            }
        });

        try
        {
            // Mientras no se cancele
            while (!token.IsCancellationRequested)
            {
                TcpClient client;
                try
                {
                    // Pasar un token para que el cancellation desbloquee este await
                    // Espera por la conexión y devuelve un cliente.
                    client = await listener.AcceptTcpClientAsync(token);
                }
                catch (OperationCanceledException)
                {
                    break; // graceful shutdown
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Server] Accept error: {ex.Message}");
                    continue;
                }

                // Fire and forget, pero con token para poder cerrar el hilo.
                // Este es el manejador de clientes
                _ = HandleClientAsync(client, token);
            }
        }
        finally
        {
            try { listener.Stop(); } catch { }
            Console.WriteLine("[Server] Stopped.");
        }

        // Por las dudas esperamos por el consoleWatcher a que termine
        try { await consoleWatcher; } catch { }
    }
    
    // Manejador de clientes
    static async Task HandleClientAsync(TcpClient client, CancellationToken token)
    {
        using (client)
            // Rescatamos el NetworkStream del TcpClient
        using (NetworkStream stream = client.GetStream())
        {
            try
            {
                // Leemos en bloques de 4KiB
                byte[] buffer = new byte[4096];
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, token);
                if (bytesRead == 0) return; // Si no leyó nada se perdió la comunicación

                string request = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine("\n--- RECEIVED REQUEST ---");
                bool hasCookie = request.Contains("Cookie: SessionID=XYZ-12345");
                Console.WriteLine(hasCookie ? "Cookie detected!" : "No cookie found. Sending one...");

                string responseBody = hasCookie
                    ? "<html><body><h1>Welcome back! You have the cookie.</h1></body></html>"
                    : "<html><body><h1>Hello! I've given you a cookie.</h1></body></html>";

                string setCookieHeader = "Set-Cookie: SessionID=XYZ-12345; " +
                                         "Expires=" + DateTime.UtcNow.AddMinutes(5).ToString("R") + "; " +
                                         "Path=/; Domain=localhost; HttpOnly; SameSite=Lax\r\n";
                
                
                string responseHeader = "HTTP/1.1 200 OK\r\n" +
                                        "Content-Type: text/html\r\n" +
                                        "Content-Length: " + Encoding.UTF8.GetByteCount(responseBody) + "\r\n" +
                                        // Agregamos la cookie si existe
                                        (hasCookie ? "" : setCookieHeader) +
                                        "Connection: close\r\n\r\n";

                byte[] responseBytes = Encoding.UTF8.GetBytes(responseHeader + responseBody);
                await stream.WriteAsync(responseBytes, 0, responseBytes.Length, token);
                // Opcional: mostrar el protocolo
                Console.WriteLine(responseHeader  + responseBody);
                Console.ResetColor();
            }
            catch (OperationCanceledException)
            {
                // Server is shutting down mid-request; just exit quietly
            }
            catch (ObjectDisposedException)
            {
                // Socket got closed during shutdown
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Server] Client handler error: {ex.Message}");
            }
        }
    }
}
