using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace _07_HTTP_00_04_server_https;

public sealed record Session(string Username, bool IsAdmin, DateTime Expires);

public sealed class SessionStore
{
    private readonly ConcurrentDictionary<string, Session> _sessions = new();
    private readonly TimeSpan _ttl;
    public SessionStore(TimeSpan ttl) => _ttl = ttl;

    public string Create(string username, bool isAdmin)
    {
        var id = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        _sessions[id] = new Session(username, isAdmin, DateTime.UtcNow + _ttl);
        return id;
    }

    public Session? TryGet(string id)
    {
        if (!_sessions.TryGetValue(id, out var s)) return null;
        if (s.Expires < DateTime.UtcNow) { _sessions.TryRemove(id, out _); return null; }
        return s;
    }

    public void Destroy(string id) => _sessions.TryRemove(id, out _);
}

public static class Cookies
{
    public const string SessionCookieName = "sid";

    public static string? GetSessionId(Dictionary<string, string> cookies)
        => cookies.TryGetValue(SessionCookieName, out var v) ? v : null;

    public static string BuildSessionCookie(string id, bool secure)
        => $"{SessionCookieName}={id}; Path=/; HttpOnly; SameSite=Strict" + (secure ? "; Secure" : "");

    public static string ClearSessionCookie(bool secure)
        => $"{SessionCookieName}=; Path=/; Max-Age=0; HttpOnly; SameSite=Strict" + (secure ? "; Secure" : "");
}