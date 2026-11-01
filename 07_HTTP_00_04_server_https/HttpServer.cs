using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Web;

namespace _07_HTTP_00_04_server_https;

public sealed class HttpServer
{
    private readonly ServerConfig _cfg;
    private readonly UserStore _users;
    private readonly SessionStore _sessions;
    private readonly AccessLog _log;

    private readonly X509Certificate2 _cert;
    private TcpListener? _http;
    private TcpListener? _https;

    public HttpServer(ServerConfig cfg, UserStore users, SessionStore sessions, AccessLog log)
    {
        _cfg = cfg; _users = users; _sessions = sessions; _log = log;
        _cert = new X509Certificate2(cfg.CertPath, cfg.CertPassword);
    }

    public async Task StartAsync(CancellationToken token)
    {
        _http  = new TcpListener(_cfg.BindIp, _cfg.HttpPort);
        _https = new TcpListener(_cfg.BindIp, _cfg.HttpsPort);
        _http.Start();
        _https.Start();

        Console.WriteLine($"HTTP  on http://{_cfg.BindIp}:{_cfg.HttpPort}  -> redirect");
        Console.WriteLine($"HTTPS on https://{_cfg.BindIp}:{_cfg.HttpsPort}");
        Console.WriteLine($"Content root: {Path.GetFullPath(_cfg.ContentRoot)}");

        var tHttp  = AcceptLoop(_http,  secure: false, token);
        var tHttps = AcceptLoop(_https, secure: true,  token);

        try { await Task.WhenAll(tHttp, tHttps); }
        catch (OperationCanceledException) { }
        finally
        {
            try { _http.Stop();  } catch { }
            try { _https.Stop(); } catch { }
            Console.WriteLine("[Server] Stopped.");
        }
    }

    private async Task AcceptLoop(TcpListener listener, bool secure, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            TcpClient client;
            try { client = await listener.AcceptTcpClientAsync(token); }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) { Console.WriteLine($"[Accept] {ex.Message}"); continue; }

            _ = Task.Run(() => HandleClientAsync(client, secure, token), token);
        }
    }

    private async Task HandleClientAsync(TcpClient client, bool secure, CancellationToken token)
    {
        Stream stream = client.GetStream();
        try
        {
            if (secure)
            {
                var ssl = new SslStream(stream, leaveInnerStreamOpen: false);
                await ssl.AuthenticateAsServerAsync(new SslServerAuthenticationOptions
                {
                    ServerCertificate = _cert,
                    EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                    ClientCertificateRequired = false,
                }, token);
                stream = ssl;
            }

            if (!secure)
            {
                await HandleHttpRedirectAsync(stream, token);
                return;
            }

            await HandleHttpsRequestAsync(stream, client, token);
        }
        catch (OperationCanceledException) { }
        catch (IOException) { }
        catch (AuthenticationException) { }
        catch (Exception ex) { Console.WriteLine($"[Handler] {ex.GetType().Name}: {ex.Message}"); }
        finally { try { client.Close(); } catch { } }
    }

    // ------- HTTP: redirect everything to HTTPS --------
    private async Task HandleHttpRedirectAsync(Stream stream, CancellationToken token)
    {
        var reader = new LineReader(stream);
        var reqLine = await reader.ReadLineAsync(token);
        if (string.IsNullOrEmpty(reqLine)) return;

        string? line;
        while (!string.IsNullOrEmpty(line = await reader.ReadLineAsync(token))) { /* drain headers */ }

        var parts = reqLine.Split(' ');
        var target = parts.Length >= 2 ? parts[1] : "/";
        var hostHeader = _cfg.BindIp.ToString(); // we don't trust Host anyway

        var loc = $"https://{hostHeader}:{_cfg.HttpsPort}{target}";
        await FileTransfer.SendRedirectAsync(stream, token, loc);
    }

    // ------- HTTPS: main dispatcher --------
    private async Task HandleHttpsRequestAsync(Stream stream, TcpClient client, CancellationToken token)
    {
        var ip = (client.Client.RemoteEndPoint as IPEndPoint)?.Address.ToString() ?? "?";

        var reader = new LineReader(stream);
        var reqLine = await reader.ReadLineAsync(token);
        if (string.IsNullOrEmpty(reqLine)) return;

        var parts = reqLine.Split(' ');
        if (parts.Length < 3) { await FileTransfer.SendStatusAsync(stream, token, "400 Bad Request"); return; }

        var method = parts[0];
        var target = parts[1];
        int q = target.IndexOf('?'); if (q >= 0) target = target[..q];

        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var cookies = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string? line;
        int contentLength = 0;
        while (!string.IsNullOrEmpty(line = await reader.ReadLineAsync(token)))
        {
            int c = line.IndexOf(':');
            if (c <= 0) continue;
            var k = line[..c].Trim();
            var v = line[(c + 1)..].Trim();
            headers[k] = v;
            if (k.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                int.TryParse(v, out contentLength);
            if (k.Equals("Cookie", StringComparison.OrdinalIgnoreCase))
                ParseCookies(v, cookies);
        }

        // Form body (we only need small bodies for login & admin forms)
        string? body = null;
        if (contentLength > 0 && contentLength <= 64 * 1024)
        {
            var buf = new byte[contentLength];
            int off = 0;
            while (off < buf.Length)
            {
                int n = await stream.ReadAsync(buf.AsMemory(off, buf.Length - off), token);
                if (n == 0) break;
                off += n;
            }
            body = Encoding.UTF8.GetString(buf, 0, off);
        }

        // Resolve session
        Session? session = null;
        var sid = Cookies.GetSessionId(cookies);
        if (sid is not null) session = _sessions.TryGet(sid);
        var userForLog = session?.Username ?? "-";

        // ---- Routes ----
        // 1) Login form (GET or POST)
        if (target.Equals("/login", StringComparison.OrdinalIgnoreCase) ||
            target.Equals("/login.html", StringComparison.OrdinalIgnoreCase))
        {
            if (method.Equals("GET", StringComparison.OrdinalIgnoreCase))
            {
                await ServeLoginPage(stream, token, error: null);
                return;
            }
            if (method.Equals("POST", StringComparison.OrdinalIgnoreCase))
            {
                var form = ParseForm(body);
                var u = form.GetValueOrDefault("user", "");
                var p = form.GetValueOrDefault("pass", "");
                if (_users.Verify(u, p))
                {
                    var real = _users.Find(u)!;
                    var newSid = _sessions.Create(real.Username, real.IsAdmin);
                    _log.AuthOk(ip, real.Username);
                    await FileTransfer.SendHtmlAsync(stream, token, "303 See Other",
                        "<html><body>Redirecting...</body></html>",
                        extraHeaders: $"Location: /index.html\r\nSet-Cookie: {Cookies.BuildSessionCookie(newSid, secure: true)}\r\n");
                    return;
                }
                _log.AuthFail(ip, u, "bad-password");
                await ServeLoginPage(stream, token, error: "Invalid username or password");
                return;
            }
            await FileTransfer.SendStatusAsync(stream, token, "405 Method Not Allowed");
            return;
        }

        // 2) Logout
        if (target.Equals("/logout", StringComparison.OrdinalIgnoreCase))
        {
            if (sid is not null) _sessions.Destroy(sid);
            _log.Logout(ip, userForLog);
            await FileTransfer.SendHtmlAsync(stream, token, "303 See Other",
                "<html><body>Bye</body></html>",
                extraHeaders: $"Location: /login\r\nSet-Cookie: {Cookies.ClearSessionCookie(secure: true)}\r\n");
            return;
        }

        // 3) Static css for login page (unauthenticated allowed)
        if (target.StartsWith("/css/login", StringComparison.OrdinalIgnoreCase))
        {
            // Not implemented; omit or serve from /wwwroot if you add files
        }

        // ---- Everything else requires a session ----
        if (session is null)
        {
            await FileTransfer.SendRedirectAsync(stream, token, "/login");
            _log.Request(ip, "-", method, target, 302);
            return;
        }

        // 4) Admin area
        if (target.StartsWith("/admin", StringComparison.OrdinalIgnoreCase))
        {
            if (!session.IsAdmin)
            {
                await FileTransfer.SendStatusAsync(stream, token, "403 Forbidden");
                _log.Request(ip, session.Username, method, target, 403);
                return;
            }
            await HandleAdminAsync(stream, token, method, target, body, ip, session);
            return;
        }

        // 5) Regular file serving under ContentRoot
        if (TryMapPathToFile(_cfg.ContentRoot, target, out var filePath))
        {
            var ct = FileTransfer.GetContentType(filePath);
            await FileTransfer.SendFileWithRangeAsync(
                stream, token, filePath, ct,
                headers.GetValueOrDefault("Range"),
                headers.GetValueOrDefault("If-None-Match"));
            _log.Request(ip, session.Username, method, target, 200);
            return;
        }

        // "/" -> index.html if present
        if (target == "/" || target.Length == 0)
        {
            var idx = Path.Combine(_cfg.ContentRoot, "index.html");
            if (File.Exists(idx))
            {
                await FileTransfer.SendFileWithRangeAsync(stream, token, idx,
                    "text/html; charset=utf-8", headers.GetValueOrDefault("Range"), null);
                _log.Request(ip, session.Username, method, target, 200);
                return;
            }
        }

        await FileTransfer.SendStatusAsync(stream, token, "404 Not Found");
        _log.Request(ip, session.Username, method, target, 404);
    }

    // ------- Admin CRUD -------
    private async Task HandleAdminAsync(Stream stream, CancellationToken token,
        string method, string target, string? body, string ip, Session session)
    {
        // simple sub-routing: /admin/ (list+form), /admin/add, /admin/delete
        if (method.Equals("POST", StringComparison.OrdinalIgnoreCase))
        {
            var form = ParseForm(body);
            var action = form.GetValueOrDefault("action", "");

            if (action == "add")
            {
                var u = form.GetValueOrDefault("user", "").Trim();
                var p = form.GetValueOrDefault("pass", "");
                var isAdmin = form.GetValueOrDefault("isAdmin", "") == "on";
                if (u.Length > 0 && p.Length >= 6)
                {
                    await _users.AddOrUpdateAsync(u, p, isAdmin);
                }
            }
            else if (action == "delete")
            {
                var u = form.GetValueOrDefault("user", "");
                if (!u.Equals(session.Username, StringComparison.OrdinalIgnoreCase))
                    await _users.RemoveAsync(u);
            }
            else if (action == "password")
            {
                var u = form.GetValueOrDefault("user", "");
                var p = form.GetValueOrDefault("pass", "");
                if (u.Length > 0 && p.Length >= 6)
                {
                    var existing = _users.Find(u);
                    var isAdmin = existing?.IsAdmin ?? false;
                    await _users.AddOrUpdateAsync(u, p, isAdmin);
                }
            }

            await FileTransfer.SendHtmlAsync(stream, token, "303 See Other", "",
                extraHeaders: "Location: /admin/\r\n");
            _log.Request(ip, session.Username, method, target, 303);
            return;
        }

        // GET: list users
        var sb = new StringBuilder();
        sb.Append("<!doctype html><html><head><meta charset='utf-8'><title>Admin</title></head><body>");
        sb.Append("<h1>User management</h1>");
        sb.Append("<p><a href='/logout'>Log out</a> &mdash; <a href='/index.html'>Home</a></p