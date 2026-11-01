using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

// Autenticación. El servidor ve cómo viene la solicitud. Si es un GET en / (raiz)
// dice que no hay nada. Si es un GET en /loginSi es un GET en /login lo redirige
// a un formulario en el propio login para hacer POST username password.
// Si va a Resource ve si está loguedo (tiene cookie válida). Si no es válida lo
// manda al /login. Si es válida le permite acceder al recurso.
// No acepta query.
//
//                   Incoming Request
//                           |
//               +-----------+-----------+
//               |                       |
//          Path: "/login"       Path: "/resource"
//              |                        |
//      +-------+-------+                |
//      |               |                |
// Method: GET     Method: POST          |
//     |               |                 |
// Sends HTML      Parses Form       Is Cookie ID
// Login Form     Data (user/pass)   Valid in Dict?
//                     |                 |
//              +------+------+       +--+--+
//              |             |       |     |
//           Valid        Invalid   [YES]  [NO]
//             |             |        |     |
//        Gen GUID       401 Err Welcome Redirects
//        & Redirect             Message to /login
//
// conectarse a http://localhost:8080/login
// formulario con user:user, pass:pass
// devuelve Cookie: sessionid={guid}
// Luego redirige a /resource.
// hereda de httpGetAuthenticate03
internal class Program
{
    private static readonly ConcurrentDictionary<string, string> Sessions = new(); // sessionId (cookie) -> username

    private static async Task Main()
    {
        var listener = new TcpListener(IPAddress.Any, 8080);
        listener.Start();
        Console.WriteLine("Listening on http://localhost:8080");
        Console.WriteLine("Type 'exit' + Enter to stop the server.");

        // Un único CTS para toda la vida del servidor
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        // Tarea de background lee la consola y cancela al ver "exit"
        var consoleWatcher = Task.Run(() =>
        {
            while (!token.IsCancellationRequested)
            {
                var line = Console.ReadLine();
                if (line == null) return; // stdin cerrado
                if (line.Trim().Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("[Server] Shutdown requested from console...");
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
    } // Fin Main

    private static async Task HandleClientAsync(TcpClient client, CancellationToken token)
    {
        try
        {
            using var stream = client.GetStream();
            var buffer = new byte[8192];
            var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, token);
            if (bytesRead == 0) return;

            // leemos toda la solicitud en un string
            var request = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            Console.WriteLine("Received request:\n" + request);

            // lo parseamos en método (GET,POST) y el URL.
            var lines = request.Split("\r\n");
            var requestLine = lines[0];
            var method = requestLine.Split(' ')[0];
            var url = requestLine.Split(' ')[1];
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var body = "";

            // Procesamos hasta el fin de los headers
            var i = 1;
            while (i < lines.Length && !string.IsNullOrWhiteSpace(lines[i]))
            {
                // parseamos los metadatos
                var parts = lines[i].Split(":", 2);
                if (parts.Length == 2)
                    headers[parts[0].Trim()] = parts[1].Trim();
                i++;
            }

            // si es un post, como en un formulario, los datos están después del header \r\n
            if (method == "POST" && i + 1 < lines.Length)
                body = string.Join("\r\n", lines[(i + 1)..]);

            // a ver si está autenticado (si tiene SessionId = galletita)
            string? sessionId = null;
            if (headers.TryGetValue("Cookie", out var cookieHeader))
            {
                var cookies = cookieHeader.Split("; ");
                foreach (var cookie in cookies)
                {
                    var kv = cookie.Split("=", 2);
                    if (kv.Length == 2 && kv[0] == "SessionId")
                        sessionId = kv[1];
                }
            }
            
            // Si es un get le mandamos el formulario
            if (url == "/login" && method == "GET")
            {
                await SendLoginForm(stream, token);
            }
            // Si es un  POST viene del formulario
            else if (url == "/login" && method == "POST")
            {
                var form = ParseFormData(body);
                if (form.TryGetValue("username", out var username) && form.TryGetValue("password", out var password))
                {
                    if (username == "user" && password == "pass")
                    {
                        // Creamos un id
                        sessionId = Guid.NewGuid().ToString();
                        // Y se lo asignamos al usuario
                        Sessions[sessionId] = username;
                        // Y respondemos con galletita
                        await SendResponse(stream, token, "302 Found",
                            $"Location: /resource\r\nSet-Cookie: SessionId={sessionId}; HttpOnly\r\n", "");
                    }
                    else  // sino entonces no autorizado
                    {
                        await SendResponse(stream, token, "401 Unauthorized", "", "<h1>Invalid credentials</h1>");
                    }
                }
                else // si no hay usuario y password ... 
                {
                    await SendResponse(stream, token, "400 Bad Request", "", "<h1>Bad form</h1>");
                }
            }
            else if (url == "/resource") // sino vamos al recurso
            {
                // verificamos la galletita
                if (sessionId != null && Sessions.ContainsKey(sessionId))
                {
                    var username = Sessions[sessionId];
                    await SendResponse(stream, token, "200 OK", "", $"<h1>Welcome, {username}</h1><p>This is your resource.</p>");
                }
                else // Sin galletita vba al login form
                {
                    await SendLoginForm(stream, token);
                }
            }
            else // Sino está buscando algo que acá no hay ....
            {
                await SendResponse(stream, token, "404 Not Found", "", "<h1>Not found</h1>");
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
            // terminamos u chaucito 
            try { client.Close(); } catch { }
        }
    }

    private static async Task SendLoginForm(NetworkStream stream, CancellationToken token)
    {
        var html = @"<html><body>
            <form method='POST' action='/login'>
                <label>Username: <input name='username' /></label><br/>
                <label>Password: <input type='password' name='password' /></label><br/>
                <input type='submit' value='Login'/>
            </form>
        </body></html>";
        await SendResponse(stream, token, "200 OK", "", html);
    }

    private static Dictionary<string, string> ParseFormData(string body)
    {
        var result = new Dictionary<string, string>();
        var pairs = body.Split('&');
        foreach (var pair in pairs)
        {
            var kv = pair.Split('=', 2);
            if (kv.Length == 2)
                result[WebUtility.UrlDecode(kv[0])] = WebUtility.UrlDecode(kv[1]);
        }

        return result;
    }

    private static async Task SendResponse(NetworkStream stream, CancellationToken token,
        string status, string extraHeaders, string body)
    {
        var bodyBytes = Encoding.UTF8.GetBytes(body);
        var response = $"HTTP/1.1 {status}\r\n" +
                       $"Content-Type: text/html\r\n" +
                       $"Content-Length: {bodyBytes.Length}\r\n" +
                       $"{extraHeaders}" +
                       "Connection: close\r\n" +
                       "\r\n";
        var headerBytes = Encoding.UTF8.GetBytes(response);
        await stream.WriteAsync(headerBytes, token);
        await stream.WriteAsync(bodyBytes, token);
    }
}