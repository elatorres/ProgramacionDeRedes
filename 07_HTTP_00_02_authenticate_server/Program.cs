using System.Net;
using System.Net.Sockets;
using System.Text;

// Este es un servidor web con autenticación.
// Accedemos a la página  http://localhost:8080/ y nos pide usuario y clave
// en un formulario creado por el navegador
// si damos usuario y clave válido nos permite acceder al recurso
// Copiar o mover los archivos secret.html y secret.txt al directorio donde se genera el código Debug o Release
// httpGetAuthenticate01
internal class Program
{
    // declaramos usuario y clave válidos
    private static readonly Dictionary<string, string> validUsers = new()
    {
        { "admin", "password123" }
    };

    // Creamos un hash para guardar las sesiones
    private static readonly HashSet<string> validSessions = new();

    private static async Task Main()
    {
        // Creamos el listener que espera la conexión del cliente
        TcpListener listener = new(IPAddress.Loopback, 8080);
        listener.Start();
        Console.WriteLine("Listening on http://localhost:8080/");
        Console.WriteLine("Type 'exit' + Enter to stop the server.");

        // Un único CTS para toda la vida del servidor
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        // Tarea de background lee la consola y cancela al ver "exit"
        var consoleWatcher = Task.Run(() =>
        {
            while (!token.IsCancellationRequested)
            {
                // Sólo escucha por exit
                var line = Console.ReadLine();
                if (line == null) return; // stdin cerrado
                if (line.Trim().Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("[Server] Shutdown requested from console...");
                    // Cancelamos 
                    cts.Cancel();
                    return;
                }
            }
        });

        try
        {
            // mientras no cancelemos
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
            // El client handler es un servidor web minimalista
            // Extraigo el networkStream del TcpClient, y asigno un streamreader y un streamwriter
            // con codificación ASCII
            using var stream = client.GetStream();
            using var reader = new StreamReader(stream, Encoding.ASCII);
            using var writer = new StreamWriter(stream, Encoding.ASCII) { AutoFlush = true };

            // Prepara unas estructuras
            string? line;
            var request = new List<string>();
            // y lee líneas del lector de stream hasta que haya algo
            while (!string.IsNullOrWhiteSpace(line = await reader.ReadLineAsync(token)))
                request.Add(line);

            // Si no hay solicitudes termina
            if (request.Count == 0)
                return;

            // Mostramos la solicitud recibida
            Console.WriteLine(">>>>>>>>>>>>>>>>>>>> REQUEST: <<<<<<<<<<<<<<<<<<<");
            foreach (var h in request) Console.WriteLine(h);

            // parseamos la solicitud. Extrae la primer línea "GET /index.html HTTP/1.1 ."
            var requestLine = request[0];
            // buscamos la primer línea que tenga autorización .... o nulo ...
            var authorization = request.FirstOrDefault(h => h.StartsWith("Authorization: "));
            // buscamos la galletita ... y la extraemos ...
            var cookie = request.FirstOrDefault(h => h.StartsWith("Cookie: "));
            // cortamos la cadena y la convertimos en un array de cadenas separados en
            // los espacios ... toma "GET /index.html HTTP/1.1." y lo convierte en
            // ["GET", "/index.html", "HTTP/1.1", "."] y extrae el segundo elemento
            // (posición 1) 
            var path = requestLine.Split(' ')[1];
            // todavía no se autenticó
            var authenticated = false;

            // Verificamos la cookie session
            if (cookie != null && cookie.Contains("sessionid="))
            {
                // Si hay una galletita y coincide con la que tengo en mi base de datos de sesiones
                var sid = cookie.Split("sessionid=")[1].Split(';')[0].Trim();
                if (validSessions.Contains(sid)) // verifica si está 
                    authenticated = true; // entonces está autenticado
            }

            // Verificar la autenticación básica
            if (!authenticated && authorization != null)
            {
                // Un Basic auth header es del tipo: "Authorization: Basic [encoded_string]"
                var encoded = authorization.Split(' ')[2]; // 0:"Authorization:", 1:"Basic", 2:"[encoded_string]" <= Se extrae este ...
                var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));  // Acá obtenemos "username:password"
                var parts = decoded.Split(':'); // Separamos 0:"username", 1:"password"
                // A ver si están ...
                if (parts.Length == 2 && validUsers.TryGetValue(parts[0], out var pw) && pw == parts[1])
                {
                    // Si sí están ... por las dudas
                    authenticated = true;
                    
                    // Generamos un session ID y lo guardamos
                    var sessionId = Guid.NewGuid().ToString();
                    validSessions.Add(sessionId);
                    // y le respondemos con la galletita/sessionId ...
                    // y redirigimos al recurso ...
                    await SendRedirectWithCookie(writer, sessionId);
                    return;
                }
            }
            
            // porque no tiene cookie ni user:pass
            if (!authenticated)
            {
                // Respondemos que no tiene acceso
                await SendUnauthorized(writer);
                return;
            }

            // Si está autenticado: Verificamos que pida el index.html
            if (path == "/" || path == "/index.html") // cualquiera de los dos me da el index
                // y se lo damos
                await SendHtml(writer,
                    "<h1>Welcome!</h1><p>Try <a href='/secret.html'>secret.html</a> or <a href='/secret.txt'>secret.txt</a></p>");
            // si pide el archivo
            else if (path == "/secret.html")
                // se lo damos
                await SendHtmlFile(writer, "secret.html");
            // si pide el archivo
            else if (path == "/secret.txt")
                // se lo damos
                await SendFile(writer, "secret.txt");
            else
                // si pide otra cosa
                await SendNotFound(writer); // Acá no está ...
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
            try { client.Dispose(); } catch { }
        }
    }

    private static async Task SendRedirectWithCookie(StreamWriter writer, string sessionId)
    {
        // respondemos con un 302 y lo mandamos a la raíz
        var response =
            "HTTP/1.1 302 Found\r\n" +
            $"Set-Cookie: sessionid={sessionId}; Path=/; HttpOnly\r\n" +
            "Location: /\r\n" +
            "Content-Length: 0\r\n" +
            "Connection: close\r\n\r\n";
        await writer.WriteAsync(response);
        Console.WriteLine(">>>>>>>>>>>>>>>>>>>> RESPONSE: <<<<<<<<<<<<<<<<<<<");
        Console.WriteLine(response);
    }

    private static async Task SendUnauthorized(StreamWriter writer)
    {
        // respondemos no autorizado
        var response =
            "HTTP/1.1 401 Unauthorized\r\n" +
            "WWW-Authenticate: Basic realm=\"MyServer\"\r\n" +
            "Content-Length: 0\r\n" +
            "Connection: close\r\n\r\n";
        await writer.WriteAsync(response);
        Console.WriteLine(">>>>>>>>>>>>>>>>>>>> RESPONSE: <<<<<<<<<<<<<<<<<<<");
        Console.WriteLine(response);
    }

    private static async Task SendHtml(StreamWriter writer, string html)
    {
        // Mandamos un html de memoria
        var htmlBytes = Encoding.UTF8.GetBytes(html);
        var header =
            "HTTP/1.1 200 OK\r\n" +
            "Content-Type: text/html\r\n" +
            $"Content-Length: {htmlBytes.Length}\r\n" +
            "Connection: close\r\n\r\n";
        await writer.WriteAsync(header);
        Console.WriteLine(">>>>>>>>>>>>>>>>>>>> RESPONSE: <<<<<<<<<<<<<<<<<<<");
        Console.WriteLine(header);
        await writer.BaseStream.WriteAsync(htmlBytes, 0, htmlBytes.Length);
    }

    private static async Task SendFile(StreamWriter writer, string filePath)
    {
        // mandamos un archivo de texto
        // Si no existe
        if (!File.Exists(filePath))
        {
            //mandamos un no encontrado
            await SendNotFound(writer);
            return;
        }
        // Esto sólo para archivos pequeños. Archivos grandes hay que manejar otras formas
        var content = File.ReadAllBytes(filePath); // leo la totalidad del archivo a memoria y lo mando de una
        var header =
            "HTTP/1.1 200 OK\r\n" +
            "Content-Type: text/plain\r\n" +
            $"Content-Length: {content.Length}\r\n" +
            "Connection: close\r\n\r\n";
        await writer.WriteAsync(header);
        Console.WriteLine(">>>>>>>>>>>>>>>>>>>> RESPONSE: <<<<<<<<<<<<<<<<<<<");
        Console.WriteLine(header);
        await writer.BaseStream.WriteAsync(content, 0, content.Length);
    }

    private static async Task SendHtmlFile(StreamWriter writer, string filePath)
    {
        // mandamos un archivo html
        // Si no existe
        if (!File.Exists(filePath))
        {
            //mandamos un no encontrado
            await SendNotFound(writer);
            return;
        }
        // Esto sólo para archivos pequeños. Archivos grandes hay que manejar otras formas
        var content = File.ReadAllBytes(filePath); // leo la totalidad del archivo a memoria y lo mando de una
        var header =
            "HTTP/1.1 200 OK\r\n" +
            "Content-Type: text/html\r\n" +
            $"Content-Length: {content.Length}\r\n" +
            "Connection: close\r\n\r\n";
        await writer.WriteAsync(header);
        Console.WriteLine(">>>>>>>>>>>>>>>>>>>> RESPONSE: <<<<<<<<<<<<<<<<<<<");
        Console.WriteLine(header);
        await writer.BaseStream.WriteAsync(content, 0, content.Length);
    }

    private static async Task SendNotFound(StreamWriter writer)
    {
        // Acánostá ... Envío 404 no encontrado ...
        var body = "<h1>404 Not Found</h1>";
        var bodyBytes = Encoding.UTF8.GetBytes(body);
        var header =
            "HTTP/1.1 404 Not Found\r\n" +
            "Content-Type: text/html\r\n" +
            $"Content-Length: {bodyBytes.Length}\r\n" +
            "Connection: close\r\n\r\n";
        await writer.WriteAsync(header);
        Console.WriteLine(">>>>>>>>>>>>>>>>>>>> RESPONSE: <<<<<<<<<<<<<<<<<<<");
        Console.WriteLine(header);
        await writer.BaseStream.WriteAsync(bodyBytes, 0, bodyBytes.Length);
    }
}