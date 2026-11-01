using System.Text;

namespace _07_HTTP_00_04_server_https;

public sealed class LineReader
{
    private readonly Stream _s;
    public LineReader(Stream s) => _s = s;

    public async Task<string?> ReadLineAsync(CancellationToken t)
    {
        var sb = new StringBuilder(128);
        var one = new byte[1];
        while (true)
        {
            int n = await _s.ReadAsync(one.AsMemory(0, 1), t);
            if (n == 0) return sb.Length == 0 ? null : sb.ToString();
            char c = (char)one[0];
            if (c == '\n')
            {
                if (sb.Length > 0 && sb[^1] == '\r') sb.Length--;
                return sb.ToString();
            }
            sb.Append(c);
            if (sb.Length > 16 * 1024) throw new IOException("Line too long.");
        }
    }
}