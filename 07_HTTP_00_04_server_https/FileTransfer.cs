using System.Net;

namespace _07_HTTP_00_04_server_https;

public static class FileTransfer
{
    private const long SmallFileThreshold = 1L * 1024 * 1024;
    private const int  SmallFileChunkSize = 64 * 1024;
    private const int  LargeFileChunkSize = 128 * 1024;
    private const long LargeFileThreshold = 100L * 1024 * 1024;

    public static async Task SendFileWithRangeAsync(
        Stream stream,
        CancellationToken token,
        string filePath,
        string contentType,
        string? rangeHeader,
        string? ifNoneMatch,
        bool forceAttachment = false)
    {
        if (!File.Exists(filePath)) { await SendStatusAsync(stream, token, "404 Not Found"); return; }

        var fileInfo  = new FileInfo(filePath);
        long totalSize = fileInfo.Length;
        var fileName   = Path.GetFileName(filePath);
        var etag       = $"\"{totalSize:x}-{fileInfo.LastWriteTimeUtc.Ticks:x}\"";

        if (ifNoneMatch != null && ifNoneMatch.Contains(etag, StringComparison.Ordinal))
        {
            await WriteAsync(stream, token,
                "HTTP/1.1 304 Not Modified\r\n" +
                $"ETag: {etag}\r\nContent-Length: 0\r\nConnection: close\r\n\r\n");
            return;
        }

        long start = 0, end = totalSize - 1;
        bool isRange = false;

        if (!string.IsNullOrEmpty(rangeHeader) &&
            TryParseSingleRange(rangeHeader, totalSize, out var ps, out var pe, out var ok))
        {
            if (!ok)
            {
                await WriteAsync(stream, token,
                    "HTTP/1.1 416 Range Not Satisfiable\r\n" +
                    $"Content-Range: bytes */{totalSize}\r\nContent-Length: 0\r\nConnection: close\r\n\r\n");
                return;
            }
            start = ps; end = pe; isRange = true;
        }

        long len = end - start + 1;
        if (len <= 0) { await SendStatusAsync(stream, token, "416 Range Not Satisfiable"); return; }

        int chunkSize = len <= SmallFileThreshold ? (int)len
            : len >= LargeFileThreshold ? LargeFileChunkSize : SmallFileChunkSize;

        var disposition = forceAttachment || ShouldForceAttachment(contentType, totalSize)
            ? $"Content-Disposition: attachment; filename=\"{fileName}\"\r\n" : "";

        string header = isRange
            ? "HTTP/1.1 206 Partial Content\r\n" +
              $"Content-Type: {contentType}\r\n{disposition}" +
              $"Content-Length: {len}\r\nContent-Range: bytes {start}-{end}/{totalSize}\r\n" +
              $"Accept-Ranges: bytes\r\nETag: {etag}\r\nConnection: close\r\n\r\n"
            : "HTTP/1.1 200 OK\r\n" +
              $"Content-Type: {contentType}\r\n{disposition}" +
              $"Content-Length: {totalSize}\r\n" +
              $"Accept-Ranges: bytes\r\nETag: {etag}\r\nConnection: close\r\n\r\n";

        await WriteAsync(stream, token, header);

        await using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read,
            chunkSize <= 4096 ? 4096 : chunkSize, FileOptions.Asynchronous | FileOptions.SequentialScan);
        if (start > 0) fs.Seek(start, SeekOrigin.Begin);

        var buffer = new byte[chunkSize];
        long remaining = len;
        while (remaining > 0)
        {
            token.ThrowIfCancellationRequested();
            int toRead = (int)Math.Min(buffer.Length, remaining);
            int read = await fs.ReadAsync(buffer.AsMemory(0, toRead), token);
            if (read == 0) break;
            await stream.WriteAsync(buffer.AsMemory(0, read), token);
            remaining -= read;
        }
        await stream.FlushAsync(token);
    }

    public static async Task SendRedirectAsync(Stream s, CancellationToken t, string location, bool secureCookieHint = false)
        => await WriteAsync(s, t,
            "HTTP/1.1 302 Found\r\n" +
            $"Location: {location}\r\nContent-Length: 0\r\nConnection: close\r\n\r\n");

    public static async Task SendStatusAsync(Stream s, CancellationToken t, string status, string extra = "")
        => await WriteAsync(s, t,
            $"HTTP/1.1 {status}\r\n{extra}Content-Length: 0\r\nConnection: close\r\n\r\n");

    public static async Task SendHtmlAsync(Stream s, CancellationToken t, string status, string html,
                                           string extraHeaders = "", string contentType = "text/html; charset=utf-8")
    {
        var body = System.Text.Encoding.UTF8.GetBytes(html);
        await WriteAsync(s, t,
            $"HTTP/1.1 {status}\r\nContent-Type: {contentType}\r\n" +
            $"Content-Length: {body.Length}\r\n{extraHeaders}Connection: close\r\n\r\n");
        await s.WriteAsync(body, t);
        await s.FlushAsync(t);
    }

    private static async Task WriteAsync(Stream s, CancellationToken t, string text)
        => await s.WriteAsync(System.Text.Encoding.ASCII.GetBytes(text), t);

    public static string GetContentType(string filePath) => Path.GetExtension(filePath).ToLowerInvariant() switch
    {
        ".html" or ".htm" => "text/html; charset=utf-8",
        ".css"            => "text/css; charset=utf-8",
        ".js"             => "application/javascript; charset=utf-8",
        ".json"           => "application/json; charset=utf-8",
        ".txt" or ".log"  => "text/plain; charset=utf-8",
        ".png"            => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".gif"            => "image/gif",
        ".svg"            => "image/svg+xml",
        ".webp"           => "image/webp",
        ".pdf"            => "application/pdf",
        ".zip"            => "application/zip",
        ".mp4"            => "video/mp4",
        ".mp3"            => "audio/mpeg",
        _                 => "application/octet-stream",
    };

    private const long AttachmentSizeThreshold = 32L * 1024 * 1024;
    private static bool ShouldForceAttachment(string ct, long size)
    {
        if (size > AttachmentSizeThreshold) return true;
        return !(ct.StartsWith("text/") || ct.StartsWith("image/") ||
                 ct.StartsWith("application/json") || ct.StartsWith("application/javascript"));
    }

    private static bool TryParseSingleRange(string hv, long total, out long start, out long end, out bool ok)
    {
        start = 0; end = total - 1; ok = true;
        int eq = hv.IndexOf('='); if (eq < 0) return false;
        if (!hv[..eq].Trim().Equals("bytes", StringComparison.OrdinalIgnoreCase)) return false;
        var spec = hv[(eq + 1)..].Trim();
        if (spec.Contains(',')) return false;
        int dash = spec.IndexOf('-'); if (dash < 0) return false;
        var a = spec[..dash].Trim(); var b = spec[(dash + 1)..].Trim();

        if (a.Length == 0)
        {
            if (!long.TryParse(b, out var suf) || suf <= 0) { ok = false; return true; }
            if (suf > total) suf = total;
            start = total - suf; end = total - 1; return true;
        }
        if (!long.TryParse(a, out start) || start < 0) return false;
        if (start >= total) { ok = false; return true; }
        if (b.Length == 0) { end = total - 1; return true; }
        if (!long.TryParse(b, out end) || end < start) return false;
        if (end >= total) end = total - 1;
        return true;
    }
}