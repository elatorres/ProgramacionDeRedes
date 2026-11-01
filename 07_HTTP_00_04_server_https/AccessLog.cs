
namespace _07_HTTP_00_04_server_https;

public enum LogMode { All, LoginOnly }

public sealed class AccessLog
{
    private readonly object _gate = new();
    private bool _enabled;
    private LogMode _mode;
    private string _path;

    public AccessLog(bool enabled, string mode, string path)
        => Reconfigure(enabled, mode, path);

    public void Reconfigure(bool enabled, string mode, string path)
    {
        lock (_gate)
        {
            _enabled = enabled;
            _mode = mode.Equals("login", StringComparison.OrdinalIgnoreCase)
                ? LogMode.LoginOnly : LogMode.All;
            _path = path;
        }
    }

    public void AuthFail(string ip, string user, string reason)
        => Write($"AUTH-FAIL ip={ip} user={Safe(user)} reason={reason}");

    public void AuthOk(string ip, string user)
        => Write($"AUTH-OK   ip={ip} user={Safe(user)}");

    public void Logout(string ip, string user)
        => Write($"LOGOUT    ip={ip} user={Safe(user)}");

    public void Request(string ip, string user, string method, string target, int status)
    {
        if (CurrentMode() != LogMode.All) return;
        Write($"REQ       ip={ip} user={Safe(user)} {method} {Safe(target)} {status}");
    }

    private LogMode CurrentMode() { lock (_gate) return _mode; }

    private void Write(string line)
    {
        if (!_enabled) return;
        // "login" mode only logs AUTH-FAIL / AUTH-OK / LOGOUT
        lock (_gate)
        {
            var isAuthLine = line.Contains("AUTH-FAIL") || line.Contains("AUTH-OK") || line.Contains("LOGOUT");
            if (_mode == LogMode.LoginOnly && !isAuthLine) return;

            try
            {
                File.AppendAllText(_path,
                    $"{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss.fffZ} {line}{Environment.NewLine}");
            }
            catch { /* logging must never crash the server */ }
        }
    }

    private static string Safe(string s) => s.Replace(' ', '_').Replace('\r', '_').Replace('\n', '_');
}