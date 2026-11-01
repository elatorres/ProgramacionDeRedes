using System.Net;
using System.Text;

internal class SimpleHttpClient
{
    private static async Task Main()
    {
        const string defaultUrl = "https://httpbin.org/cookies/set?sessionid=xyz123&theme=dark";

        // Jar lives for the whole app
        var cookieJar = new CookieContainer();

        // ---- Created ONCE, before the loop ----
        var primaryHandler = new HttpClientHandler
        {
            CookieContainer = cookieJar,
            UseCookies = true,
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };

        // The logging handler also lives for the whole app; it wraps primaryHandler.
        var loggingHandler = new RequestLoggingHandler(primaryHandler);

        // HttpClient lives for the whole app. disposeHandler:false so disposing
        // the client never disposes the handlers we manage ourselves.
        var client = new HttpClient(loggingHandler, disposeHandler: false);

        try
        {
            while (true)
            {
                Console.Write($"Enter URL (press Enter for default '{defaultUrl}'): ");
                Console.WriteLine("Type 'exit' to quit.\n");
                var inputUrl = Console.ReadLine()?.Trim() ?? string.Empty;
                if (inputUrl.Equals("exit", StringComparison.OrdinalIgnoreCase))
                    break;

                var targetUrl = string.IsNullOrWhiteSpace(inputUrl) ? defaultUrl : inputUrl;

                if (!Uri.TryCreate(targetUrl, UriKind.Absolute, out var targetUri))
                {
                    Console.WriteLine($"Error: '{targetUrl}' is not a valid absolute URL.");
                    continue;   // don't return — stay in the loop
                }

                try
                {
                    // Per-request disposable
                    using var request = new HttpRequestMessage(HttpMethod.Get, targetUri);
                    request.Headers.Add("User-Agent", "SimpleHttpClient/1.0");

                    // Per-response disposable — now correctly inside 'using'
                    using var response = await client.SendAsync(request);

                    // ... (unchanged body-reading / Set-Cookie parsing / display code) ...
                    var responsePacket = new StringBuilder();
                    responsePacket.AppendLine("==================== INCOMING HTTP RESPONSE ====================");
                    responsePacket.AppendLine(
                        $"HTTP/{response.Version} {(int)response.StatusCode} {response.ReasonPhrase}");

                    foreach (var header in response.Headers)
                        responsePacket.AppendLine($"{header.Key}: {string.Join(", ", header.Value)}");

                    foreach (var header in response.Content.Headers)
                        responsePacket.AppendLine($"{header.Key}: {string.Join(", ", header.Value)}");

                    responsePacket.AppendLine();
                    var body = await response.Content.ReadAsStringAsync();
                    responsePacket.AppendLine(body);
                    responsePacket.AppendLine("================================================================");

                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine(responsePacket.ToString());
                    Console.ResetColor();

                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine("==================== RAW SET-COOKIE HEADERS (RECEIVED) ====================");
                    if (response.Headers.TryGetValues("Set-Cookie", out var rawCookieHeaders))
                        foreach (var headerValue in rawCookieHeaders)
                        {
                            Console.WriteLine($"Raw: {headerValue}");

                            var segments = headerValue.Split(';');
                            var nameVal = segments[0].Trim().Split('=', 2);
                            var name = nameVal[0];
                            var value = nameVal.Length > 1 ? nameVal[1] : string.Empty;

                            var path = "/";
                            var domain = targetUri.Host;
                            var isSecure = false;
                            var isHttpOnly = false;
                            var expires = DateTime.MinValue;

                            for (var i = 1; i < segments.Length; i++)
                            {
                                var directive = segments[i].Trim();
                                if (directive.StartsWith("Path=", StringComparison.OrdinalIgnoreCase))
                                    path = directive.Substring(5).Trim();
                                else if (directive.StartsWith("Domain=", StringComparison.OrdinalIgnoreCase))
                                    domain = directive.Substring(7).Trim();
                                else if (directive.Equals("Secure", StringComparison.OrdinalIgnoreCase))
                                    isSecure = true;
                                else if (directive.Equals("HttpOnly", StringComparison.OrdinalIgnoreCase))
                                    isHttpOnly = true;
                                else if (directive.StartsWith("Expires=", StringComparison.OrdinalIgnoreCase))
                                    if (DateTime.TryParse(directive.Substring(8).Trim(), out var parsedExpires))
                                        expires = parsedExpires;
                            }

                            Console.WriteLine($"  • Parsed Name: {name}");
                            Console.WriteLine($"    Value:       {value}");
                            Console.WriteLine($"    Domain:      {domain}");
                            Console.WriteLine($"    Path:        {path}");
                            Console.WriteLine($"    HttpOnly:    {isHttpOnly}");
                            Console.WriteLine($"    Secure:      {isSecure}");
                            Console.WriteLine(
                                $"    Expires:     {(expires == DateTime.MinValue ? "Session" : expires.ToString("R"))}");
                            Console.WriteLine();

                            try
                            {
                                var manualCookie = new Cookie(name, value, path, domain)
                                {
                                    Secure = isSecure,
                                    HttpOnly = isHttpOnly
                                };
                                if (expires != DateTime.MinValue) manualCookie.Expires = expires;
                                cookieJar.Add(manualCookie);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"    [Notice: Could not insert into CookieContainer: {ex.Message}]");
                            }
                        }
                    else
                        Console.WriteLine("No 'Set-Cookie' headers present in response.");

                    Console.WriteLine("===========================================================================");
                    Console.ResetColor();

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("==================== COOKIES IN COOKIECONTAINER ====================");
                    var cookies = cookieJar.GetCookies(targetUri);

                    if (cookies.Count == 0)
                        Console.WriteLine("No cookies stored for this URI domain/path.");
                    else
                        foreach (Cookie c in cookies)
                        {
                            Console.WriteLine($"  • Name:     {c.Name}");
                            Console.WriteLine($"    Value:    {c.Value}");
                            Console.WriteLine($"    Domain:   {c.Domain}");
                            Console.WriteLine($"    Path:     {c.Path}");
                            Console.WriteLine($"    Secure:   {c.Secure}");
                            Console.WriteLine($"    HttpOnly: {c.HttpOnly}");
                            Console.WriteLine(
                                $"    Expires:  {(c.Expires == DateTime.MinValue ? "Session (on close)" : c.Expires.ToString("R"))}");
                            Console.WriteLine();
                        }

                    Console.WriteLine("====================================================================");
                    Console.ResetColor();
                }
                catch (ObjectDisposedException ex)
                {
                    // Should never happen now, but surfaced clearly if it does
                    Console.WriteLine($"Disposed object error: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Request failed: {ex.Message}");
                }
            }
        }
        finally
        {
            // Dispose exactly once, after the loop, in the right order.
            client.Dispose();
            loggingHandler.Dispose();   // disposes primaryHandler too (it's the InnerHandler)
        }

        Console.WriteLine("\n=== Stored cookies ===");
        CookieCollection allCookies = cookieJar.GetAllCookies();
        Console.WriteLine($"Total cookies: {allCookies.Count}");
        Console.WriteLine(new string('-', 40));

        foreach (Cookie cook in allCookies)
        {
            Console.WriteLine($"Name:    {cook.Name}");
            Console.WriteLine($"Value:   {cook.Value}");
            Console.WriteLine($"Domain:  {cook.Domain}");
            Console.WriteLine($"Path:    {cook.Path}");
            Console.WriteLine($"Secure:  {cook.Secure}");
            Console.WriteLine($"Expires: {cook.Expires}");
            Console.WriteLine($"String:  {cook}");
            Console.WriteLine(new string('-', 40));
        }
    }

    private class RequestLoggingHandler : DelegatingHandler
    {
        public RequestLoggingHandler(HttpMessageHandler innerHandler) : base(innerHandler) { }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var sb = new StringBuilder();
            sb.AppendLine("==================== OUTGOING HTTP REQUEST ====================");
            sb.AppendLine($"{request.Method} {request.RequestUri?.PathAndQuery} HTTP/{request.Version}");
            sb.AppendLine($"Host: {request.RequestUri?.Authority}");

            foreach (var header in request.Headers)
                sb.AppendLine($"{header.Key}: {string.Join(", ", header.Value)}");

            if (request.Content != null)
            {
                foreach (var header in request.Content.Headers)
                    sb.AppendLine($"{header.Key}: {string.Join(", ", header.Value)}");
                sb.AppendLine();
                sb.AppendLine(await request.Content.ReadAsStringAsync(cancellationToken));
            }
            else
            {
                sb.AppendLine();
            }

            sb.AppendLine("===============================================================");

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(sb.ToString());
            Console.ResetColor();
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
