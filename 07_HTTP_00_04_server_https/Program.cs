using System.Text.Json;

internal static class Program
{
    public static async Task Main()
    {
        var cfg = ServerConfig.Load("appsettings.json");

        Directory.CreateDirectory(cfg.ContentRoot);
        Directory.CreateDirectory(cfg.DataDir);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(cfg.LogFilePath))!);

        var users  = new UserStore(Path.Combine(cfg.DataDir, "users.json"));
        await users.LoadAsync();

        // Ensure an admin exists
        if (users.Find("admin") is null)
        {
            var pw = PromptNewPassword("admin");
            await users.AddOrUpdateAsync("admin", pw, isAdmin: true);
            Console.WriteLine("[Init] Created default admin user. Change the password after first login.");
        }

        var accessLog = new AccessLog(cfg.LogEnabled, cfg.LogMode, cfg.LogFilePath);
        var sessions  = new SessionStore(TimeSpan.FromMinutes(cfg.SessionTimeoutMinutes));

        var server = new HttpServer(cfg, users, sessions, accessLog);

        using var cts = new CancellationTokenSource();
        var watcher = Task.Run(async () =>
        {
            while (!cts.IsCancellationRequested)
            {
                var line = await Console.In.ReadLineAsync();
                if (line is null) return;
                if (line.Trim().Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("[Server] Shutdown requested.");
                    cts.Cancel();
                    return;
                }
                if (line.Trim().Equals("reload", StringComparison.OrdinalIgnoreCase))
                {
                    cfg = ServerConfig.Load("appsettings.json");
                    accessLog.Reconfigure(cfg.LogEnabled, cfg.LogMode, cfg.LogFilePath);
                    Console.WriteLine("[Server] Config reloaded (ports require restart).");
                }
            }
        });

        try
        {
            await server.StartAsync(cts.Token);
        }
        finally
        {
            await users.SaveAsync();
            try { await watcher; } catch { }
        }
    }

    private static string PromptNewPassword(string user)
    {
        Console.Write($"New password for '{user}': ");
        var pw = ReadPassword();
        Console.WriteLine();
        return pw;
    }

    private static string ReadPassword()
    {
        var sb = new System.Text.StringBuilder();
        while (true)
        {
            var k = Console.ReadKey(intercept: true);
            if (k.Key == ConsoleKey.Enter) break;
            if (k.Key == ConsoleKey.Backspace && sb.Length > 0) sb.Length--;
            else if (!char.IsControl(k.KeyChar)) sb.Append(k.KeyChar);
        }
        return sb.ToString();
    }
}