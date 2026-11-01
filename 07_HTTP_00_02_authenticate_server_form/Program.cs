using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Web;

// conectarse a http://localhost:8080/login
// brinda formulario html (username == "admin" && password == "secret")
// también autentica por query: http://localhost:8080/login?username=admin&password=secret
// Luego redirige a /resource.
// escriba exit para salir
internal class Program
{
    // repositorio de las sesiones (cookies)
    private static readonly ConcurrentDictionary<string, string> sessions = new();

    private static async Task Main()
    {
        TcpListener listener = new(IPAddress.Any, 8080);
        listener.Start();
        Console.WriteLine("Server started on port 8080...");
        Console.WriteLine("Type 'exit' + Enter to stop the server.");

        // Un único CTS para toda la vida del servidor
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        // Tarea de fondo: lee la consola y cancela al ver "exit"
        var consoleWatcher = Task.Run(() =>
        {
            while (!token.IsCancellationRequested)
            {
                var line = Console.ReadLine();
                if (line == null) return; // stdin cerrado
                if (line.Trim().Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("[Server] Shutdown requested from console...");
                    //cancelamos
                    cts.Cancel();
                    return;
                }
            }
        });

        try
        {
            while (!token.IsCancellationRequested)
            {
                TcpClient client;
                try
                {
                    // El token desbloquea este await cuando se cancela
                    client = await listener.AcceptTcpClientAsync(token);
                }
                catch (OperationCanceledException)
                {
                    break; // apagado ordenado
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Server] Accept error: {ex.Message}");
                    continue;
                }

                // Fire and forget, pero pasando el token para que el cliente
                // en curso también se detenga en el apagado.
                _ = Task.Run(() => HandleClientAsync(client, token), token);
            }
        }
        finally
        {
            try { listener.Stop(); } catch { }
            Console.WriteLine("[Server] Stopped.");
        }

        // Dejar terminar la tarea de consola
        try { await consoleWatcher; } catch { }
    }

    private static async Task HandleClientAsync(TcpClient client, CancellationToken token)
    {
        try
        {
            using var stream = client.GetStream();
            using var reader = new StreamReader(stream, Encoding.ASCII);
            using var writer = new StreamWriter(stream, Encoding.ASCII) { AutoFlush = true };

            // lee lo enviado por el navegador línea por línea
            var requestLine = await reader.ReadLineAsync(token);
            if (requestLine == null) return; // si es nulo terminamos

            Console.WriteLine("Request: " + requestLine);
            string method, path;
            var parts = requestLine.Split(' ');
            
            // A ver si viene en query_url
            if (parts.Length < 2) return; // tiene que tener 3 partes
            // GET /login?username=admin&password=secret HTTP/1.1
            method = parts[0]; // el GET
            path = parts[1];   // la línea de url /login?username=...

            string? cookieHeader = null;
            string? line;
            // leemos el resto de las líneas hasta \r\n que es el final del header
            while (!string.IsNullOrEmpty(line = await reader.ReadLineAsync(token)))
                if (line.StartsWith("Cookie:")) // buscamos galletitas ...
                    cookieHeader = line.Substring(7).Trim(); // SI es válida es porque ya está logueado

            // A ver si existe la sesión ...
            var sessionId = GetSessionIdFromCookie(cookieHeader);
            var username = sessionId != null && sessions.ContainsKey(sessionId) ? sessions[sessionId] : null;

            // Si el path es login ...
            if (path.StartsWith("/login"))
            {
                var uri = new Uri("http://localhost" + path);
                //  parseamos el query
                var query = HttpUtility.ParseQueryString(uri.Query);
                var user = query["username"];
                var pass = query["password"];

                // Si no manda user/pass entonces lo mandamos al login form!
                if (string.IsNullOrEmpty(user) && string.IsNullOrEmpty(pass))
                {
                    // Este es el formulario html
                    var htmlForm =
                        "<html><body>" +
                        "<h2>Login Required</h2>" +
                        "<form method='GET' action='/login'>" +
                        "Username: <input type='text' name='username'><br>" +
                        "Password: <input type='password' name='password'><br>" +
                        "<input type='submit' value='Login'>" +
                        "</form>" +
                        "</body></html>";
                    
                    // Y respondemos ok y le agregamos el formulario a la respuesta
                    await writer.WriteAsync(
                        "HTTP/1.1 200 OK\r\n" +
                        "Content-Type: text/html\r\n" +
                        "Connection: close\r\n" +
                        $"Content-Length: {Encoding.ASCII.GetByteCount(htmlForm)}\r\n" +
                        "\r\n" +
                        htmlForm);
                }
                // Si mandó user/pass, los validamos
                else if (user == "admin" && pass == "secret") // Sólo tenemos un usuario
                {
                    // generamos la galletita ...
                    var newSessionId = Guid.NewGuid().ToString();
                    sessions[newSessionId] = user;

                    // Esta es la página de bienvenida!!
                    var successHtml =
                        "<html><body>" +
                        "<h2>Login successful!</h2>" +
                        "<p>You can now access your dashboard.</p>" +
                        "<a href='/resource'>Go to Secure Resource</a>" +
                        "</body></html>";

                    // Armamos la respuesta con la galletita y la página
                    await writer.WriteAsync(
                        "HTTP/1.1 200 OK\r\n" +
                        $"Set-Cookie: SessionId={newSessionId}; Path=/\r\n" +
                        "Content-Type: text/html\r\n" +
                        "Connection: close\r\n" +
                        $"Content-Length: {Encoding.ASCII.GetByteCount(successHtml)}\r\n" +
                        "\r\n" +
                        successHtml);
                }
                else  // Sino  ....
                {
                    await writer.WriteAsync(
                        "HTTP/1.1 401 Unauthorized\r\n" +
                        "Content-Type: text/plain\r\n" +
                        "Connection: close\r\n" +
                        "\r\n" +
                        "Invalid credentials.");
                }
            } // si no es login
            else if (path == "/resource") // pero es resource
            {
                // Y tiene usuario... esto es porque mando la galletita correcta
                // y lo encontramos en el diccionario ... arriba...
                if (username != null)
                    await writer.WriteAsync(
                        "HTTP/1.1 200 OK\r\n" +
                        "Content-Type: text/plain\r\n" +
                        "Connection: close\r\n" +
                        "\r\n" +
                        $"Welcome, {username}! You are authenticated.");
                else // Sino  ....
                    await writer.WriteAsync(
                        "HTTP/1.1 401 Unauthorized\r\n" +
                        "Content-Type: text/plain\r\n" +
                        "Connection: close\r\n" +
                        "\r\n" +
                        "Access denied. Please log in at /login");
            }
            else // Si el path no es ni login ni resource, no hay nada más ...
            {
                // No hay ningún otro recurso disponible en este servidor
                await writer.WriteAsync(
                    "HTTP/1.1 404 Not Found\r\n" +
                    "Content-Type: text/plain\r\n" +
                    "Connection: close\r\n" +
                    "\r\n" +
                    "Page not found.");
            }
        }
        catch (OperationCanceledException)
        {
            // Apagado del servidor en medio de una request: salir en silencio
        }
        catch (ObjectDisposedException)
        {
            // El socket se cerró durante el apagado
        }
        catch (IOException ex)
        {
            Console.WriteLine($"[Server] Client I/O error: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Server] Client handler error: {ex.Message}");
        }
        finally
        {
            // Cerramos y chau
            try { client.Close(); } catch { }
        }
    }

    private static string? GetSessionIdFromCookie(string? cookieHeader)
    {
        // si no hay, no hay
        if (cookieHeader == null) return null;

        // parseamos
        var cookies = cookieHeader.Split(';');
        // y buscamos
        foreach (var cookie in cookies)
        {
            var parts = cookie.Trim().Split('=');
            // si encontramos devolvemos
            if (parts.Length == 2 && parts[0] == "SessionId") return parts[1];
        }
        // Si no se encontró entonces null
        return null;
    }
}