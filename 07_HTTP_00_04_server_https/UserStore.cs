using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace _07_HTTP_00_04_server_https;
public sealed record UserEntry(string Username, string Salt, string Hash, bool IsAdmin);

public sealed class UserStore
{
    private readonly string _path;
    private readonly object _gate = new();
    private List<UserEntry> _users = new();

    public UserStore(string path) => _path = path;

    public async Task LoadAsync()
    {
        if (!File.Exists(_path)) { _users = new(); return; }
        var json = await File.ReadAllTextAsync(_path);
        _users = JsonSerializer.Deserialize<List<UserEntry>>(json) ?? new();
    }

    public async Task SaveAsync()
    {
        List<UserEntry> copy;
        lock (_gate) copy = _users.ToList();
        var json = JsonSerializer.Serialize(copy, new JsonSerializerOptions { WriteIndented = true });
        var tmp = _path + ".tmp";
        await File.WriteAllTextAsync(tmp, json);
        File.Move(tmp, _path, overwrite: true);
    }

    public UserEntry? Find(string username)
    {
        lock (_gate)
            return _users.FirstOrDefault(u =>
                u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<UserEntry> List()
    {
        lock (_gate) return _users.ToList();
    }

    public bool Verify(string username, string password)
    {
        var u = Find(username);
        if (u is null) return false;
        var candidate = Hash(password, Convert.FromBase64String(u.Salt));
        return CryptographicOperations.FixedTimeEquals(
            candidate, Convert.FromBase64String(u.Hash));
    }

    public async Task AddOrUpdateAsync(string username, string password, bool isAdmin)
    {
        if (string.IsNullOrWhiteSpace(username) || username.Contains(' '))
            throw new ArgumentException("invalid username");

        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Hash(password, salt);
        var entry = new UserEntry(username, Convert.ToBase64String(salt),
            Convert.ToBase64String(hash), isAdmin);

        lock (_gate)
        {
            var idx = _users.FindIndex(u =>
                u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
            if (idx >= 0) _users[idx] = entry; else _users.Add(entry);
        }
        await SaveAsync();
    }

    public async Task<bool> RemoveAsync(string username)
    {
        bool removed;
        lock (_gate) removed = _users.RemoveAll(u =>
            u.Username.Equals(username, StringComparison.OrdinalIgnoreCase)) > 0;
        if (removed) await SaveAsync();
        return removed;
    }

    private static byte[] Hash(string password, byte[] salt)
        => Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password), salt,
            iterations: 100_000, HashAlgorithmName.SHA256, outputLength: 32);
}