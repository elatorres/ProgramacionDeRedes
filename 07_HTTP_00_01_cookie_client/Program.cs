using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

class CookieJarClient
{
    // Cookie jar: El tarro de galletitas guarda para cada host una lista de
    // cadenas de valores
    static readonly Dictionary<string, List<string>> CookieJar =
        new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

    // One shared HttpClient for the whole app (recommended practice)
    static readonly HttpClient Http = new HttpClient(new HttpClientHandler
    {
        UseCookies = false,          // we manage cookies ourselves
        AllowAutoRedirect = false    // keep control of what's happening
    }) { Timeout = TimeSpan.FromSeconds(15) };

    const string DefaultUrl = "http://localhost:8080";

    static async Task Main()
    {
        Console.WriteLine("=== Interactive Cookie-Jar Client ===");
        Console.WriteLine($"Press Enter to use the default URL ({DefaultUrl}).");
        Console.WriteLine("Type 'exit' to quit.\n");

        while (true)
        {
            Console.Write("URL> ");
            string? input = Console.ReadLine();

            if (input == null) break;                       // stdin closed
            input = input.Trim();

            if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
                break;

            string target = string.IsNullOrEmpty(input) ? DefaultUrl : input;
            
            // Check uri format
            if (!Uri.TryCreate(target, UriKind.Absolute, out Uri? uri) ||
                (uri.Scheme != "http" && uri.Scheme != "https"))
            {
                Console.WriteLine($"[ERROR] Invalid URL: '{target}'. Use http:// or https://.\n");
                continue;
            }
            
            // Now do the request
            await DoRequestAsync(uri);
            Console.WriteLine();
        }
        
        // Show all collected cookies
        ShowCookieJar();
        Console.WriteLine("Bye.");
    }

    static async Task DoRequestAsync(Uri uri)
    {
        string host = uri.Host;

        // 1) Look for a stored cookie for this host
        bool haveCookie = CookieJar.TryGetValue(host, out List<string>? cookies)
                          && cookies != null && cookies.Count > 0;

        if (haveCookie)
            Console.WriteLine($"[JAR] Cookie found for {host}: {string.Join("; ", cookies!)}");
        else
            Console.WriteLine($"[JAR] No cookie stored for {host}. Doing a plain GET.");

        // 2) Build the request
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        if (haveCookie)
            request.Headers.TryAddWithoutValidation("Cookie", string.Join("; ", cookies!));

        // 3) Send it
        HttpResponseMessage response;
        try
        {
            response = await Http.SendAsync(request);
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"[ERROR] Request failed: {ex.Message}");
            return;
        }
        catch (TaskCanceledException)
        {
            Console.WriteLine("[ERROR] Request timed out.");
            return;
        }

        // 4) Read the body (truncated to keep the console readable)
        string body;
        try { body = await response.Content.ReadAsStringAsync(); }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Failed reading body: {ex.Message}");
            body = string.Empty;
        }

        Console.WriteLine($"[HTTP] {(int)response.StatusCode} {response.StatusCode}");
        if (body.Length > 0)
        {
            string preview = body.Length > 300 ? body.Substring(0, 300) + "..." : body;
            Console.WriteLine($"[BODY] {preview}");
        }

        // 5) Extract Set-Cookie headers, store them
        if (response.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? setCookies))
        {
            if (!CookieJar.ContainsKey(host))
                CookieJar[host] = new List<string>();

            foreach (string raw in setCookies)
            {
                // "name=value; Expires=...; Path=/; HttpOnly"
                // We only keep the "name=value" pair for our simple jar.
                string pair = raw.Split(';')[0].Trim();
                if (string.IsNullOrEmpty(pair)) continue;

                if (!CookieJar[host].Contains(pair))
                    CookieJar[host].Add(pair);

                Console.WriteLine($"[JAR] Stored cookie from {host}: {pair}");
            }
        }

        response.Dispose();
    }

    static void ShowCookieJar()
    {
        Console.WriteLine("\n=== Stored cookies ===");
        if (CookieJar.Count == 0)
        {
            Console.WriteLine("(empty)");
            return;
        }

        foreach (var kvp in CookieJar)
            Console.WriteLine($"{kvp.Key} => {string.Join("; ", kvp.Value)}");
    }
}