using System.Net;
using System.Text.Json;

namespace _07_HTTP_00_04_server_https;

public sealed class ServerConfig
{
    public required IPAddress BindIp { get; init; }
    public required int HttpPort { get; init; }
    public required int HttpsPort { get; init; }
    public required string CertPath { get; init; }
    public required string CertPassword { get; init; }
    public required string ContentRoot { get; init; }
    public required string DataDir { get; init; }
    public required int SessionTimeoutMinutes { get; init; }

    public bool  LogEnabled { get; init; }
    public string LogMode   { get; init; } = "all";
    public string LogFilePath { get; init; } = "logs/access.log";

    public static ServerConfig Load(string path)
    {
        var json = File.ReadAllText(path);
        using var doc = JsonDocument.Parse(json);
        var s = doc.RootElement.GetProperty("Server");
        var l = doc.RootElement.GetProperty("Logging");

        return new ServerConfig
        {
            BindIp         = IPAddress.Parse(s.GetProperty("BindIp").GetString()!),
            HttpPort       = s.GetProperty("HttpPort").GetInt32(),
            HttpsPort      = s.GetProperty("HttpsPort").GetInt32(),
            CertPath       = s.GetProperty("CertificatePath").GetString()!,
            CertPassword   = s.GetProperty("CertificatePassword").GetString()!,
            ContentRoot    = s.GetProperty("ContentRoot").GetString()!,
            DataDir        = s.GetProperty("DataDir").GetString()!,
            SessionTimeoutMinutes = s.GetProperty("SessionTimeoutMinutes").GetInt32(),
            LogEnabled     = l.GetProperty("Enabled").GetBoolean(),
            LogMode        = l.GetProperty("Mode").GetString() ?? "all",
            LogFilePath    = l.GetProperty("FilePath").GetString() ?? "logs/access.log",
        };
    }
}