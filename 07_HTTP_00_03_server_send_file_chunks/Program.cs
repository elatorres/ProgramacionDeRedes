using System.Net;
using System.Net.Sockets;
using System.Text;

// Conectarse a http://localhost:8080/index.html
// Redirige / a una imagen y la transfiere por rangos
// Poner los archivos index.html, image.png, large-beach.png y bigfile.png en el directorio del ejecutable.
// derivado de HTTP_00_04_send_file_range_00
internal class Program
{
    // Large-file friendly chunk size (32 KiB is fine; 64–128 KiB often better over LAN)
    private const int ChunkSize = 64 * 1024;

    private static async Task Main()
    {
        var listener = new TcpListener(IPAddress.Any, 8080);
        listener.Start();
        Console.WriteLine("Server running on http://localhost:8080");
        Console.WriteLine("Type 'exit' + Enter to stop the server.");

        // Un único CTS para toda la vida del servidor
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        // Tarea de fondo: lee la consola y cancela al ver "exit"
        var consoleWatcher = Task.Run(() =>
        {
            while (!token.IsCancellationRequested)
            {
                var line = Console.ReadLine();
                if (line == null) return; // stdin cerrado
                if (line.Trim().Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("[Server] Shutdown requested from console...");
                    cts.Cancel();
                    return;
                }
            }
        });

        try
        {
            while (!token.IsCancellationRequested)
            {
                TcpClient client;
                try
                {
                    client = await listener.AcceptTcpClientAsync(token);
                }
                catch (OperationCanceledException)
                {
                    break; // apagado ordenado
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Server] Accept error: {ex.Message}");
                    continue;
                }

                _ = Task.Run(() => HandleClientAsync(client, token), token);
            }
        }
        finally
        {
            try { listener.Stop(); } catch { }
            Console.WriteLine("[Server] Stopped.");
        }

        try { await consoleWatcher; } catch { }
    }

private static async Task HandleClientAsync(TcpClient client, CancellationToken token)
{
    try
    {
        using var stream = client.GetStream();

        var reader = new LineReader(stream);

        var requestLine = await reader.ReadLineAsync(token);
        if (string.IsNullOrEmpty(requestLine))
            return;

        Console.WriteLine($"Request: {requestLine}");

        var parts = requestLine.Split(' ');
        if (parts.Length < 3)
        {
            await SendSimpleStatusAsync(stream, token, "400 Bad Request");
            return;
        }
        var method = parts[0];
        var target = parts[1];

        // Strip query string (we don't use it, but it must not become part of the filename)
        int q = target.IndexOf('?');
        if (q >= 0) target = target.Substring(0, q);

        // Read headers
        string? line;
        string? rangeHeader = null;
        string? ifNoneMatch = null;
        while (!string.IsNullOrEmpty(line = await reader.ReadLineAsync(token)))
        {
            if (line.StartsWith("Range:", StringComparison.OrdinalIgnoreCase))
                rangeHeader = line.Substring(6).Trim();
            else if (line.StartsWith("If-None-Match:", StringComparison.OrdinalIgnoreCase))
                ifNoneMatch = line.Substring(14).Trim();
        }

        if (!string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase))
        {
            await SendSimpleStatusAsync(stream, token, "405 Method Not Allowed");
            return;
        }

        // "/"  redirects to the default html
        if (target == "/" || target.Length == 0)
        {
            await SendRedirectAsync(stream, token, "/index.html");
            return;
        }

        // ---- Map URL path to a local file ----
        if (!TryMapPathToFile(target, out var filePath, out var reason))
        {
            // Bad path / traversal attempt -> 400, nonexistent -> 404
            Console.WriteLine($"[Server] Rejected path '{target}': {reason}");
            await SendSimpleStatusAsync(stream, token,
                reason == "not found" ? "404 Not Found" : "400 Bad Request");
            return;
        }

        var contentType = GetContentType(filePath);
        
        // it will adapt transfer protocol according to file size
        // Serve it (range support, cancellation, big-file friendly)
        await SendFileWithRangeAsync( stream, token, filePath, contentType, rangeHeader, ifNoneMatch);
    }
    catch (OperationCanceledException) { }
    catch (ObjectDisposedException) { }
    catch (IOException ex)
    {
        Console.WriteLine($"[Server] Client I/O error: {ex.Message}");
    }
    catch (SocketException ex)
    {
        Console.WriteLine($"[Server] Socket error: {ex.SocketErrorCode} - {ex.Message}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Server] Client handler error: {ex.GetType().Name}: {ex.Message}");
    }
    finally
    {
        try { client.Close(); } catch { }
    }
}

    // --------------------------
    //        REDIRECT
    // --------------------------
    private static async Task SendRedirectAsync(NetworkStream stream, CancellationToken token, string newPath)
    {
        var response =
            "HTTP/1.1 302 Found\r\n" +
            $"Location: {newPath}\r\n" +
            "Content-Length: 0\r\n" +
            "Connection: close\r\n\r\n";

        await stream.WriteAsync(Encoding.ASCII.GetBytes(response), token);
    }

    private static async Task SendSimpleStatusAsync(NetworkStream stream, CancellationToken token, string status)
    {
        var response =
            $"HTTP/1.1 {status}\r\n" +
            "Content-Length: 0\r\n" +
            "Connection: close\r\n\r\n";

        await stream.WriteAsync(Encoding.ASCII.GetBytes(response), token);
    }

    // --------------------------
    //        RANGE SUPPORT
    // --------------------------
// Strategy thresholds
private const long SmallFileThreshold   = 1L * 1024 * 1024;   // <= 1 MiB : buffer whole file
private const int  SmallFileChunkSize   = 64 * 1024;          // streaming chunk for medium/large
private const int  LargeFileChunkSize   = 128 * 1024;         // bigger chunks for very large files
private const long LargeFileThreshold   = 100L * 1024 * 1024; // >= 100 MiB : use large chunks

private static async Task SendFileWithRangeAsync(
    NetworkStream stream,
    CancellationToken token,
    string filePath,
    string contentType,
    string? rangeHeader,
    string? ifNoneMatch)
{
    if (!File.Exists(filePath))
    {
        await SendSimpleStatusAsync(stream, token, "404 Not Found");
        return;
    }

    var fileInfo  = new FileInfo(filePath);
    long totalSize = fileInfo.Length;
    var fileName   = Path.GetFileName(filePath);
    var etag       = $"\"{totalSize:x}-{fileInfo.LastWriteTimeUtc.Ticks:x}\"";

    // ---- If-None-Match ----
    if (ifNoneMatch != null && ifNoneMatch.Contains(etag, StringComparison.Ordinal))
    {
        var notModified =
            "HTTP/1.1 304 Not Modified\r\n" +
            $"ETag: {etag}\r\n" +
            "Content-Length: 0\r\n" +
            "Connection: close\r\n\r\n";
        await stream.WriteAsync(Encoding.ASCII.GetBytes(notModified), token);
        return;
    }

    // ---- Range parsing ----
    long start = 0;
    long end = totalSize - 1;
    bool isRangeRequest = false;

    if (!string.IsNullOrEmpty(rangeHeader) &&
        TryParseSingleRange(rangeHeader, totalSize, out var parsedStart, out var parsedEnd, out var satisfiable))
    {
        if (!satisfiable)
        {
            var invalid =
                "HTTP/1.1 416 Range Not Satisfiable\r\n" +
                $"Content-Range: bytes */{totalSize}\r\n" +
                "Content-Length: 0\r\n" +
                "Connection: close\r\n\r\n";
            await stream.WriteAsync(Encoding.ASCII.GetBytes(invalid), token);
            return;
        }
        start = parsedStart;
        end = parsedEnd;
        isRangeRequest = true;
    }

    long contentLength = end - start + 1;
    if (contentLength <= 0)
    {
        await SendSimpleStatusAsync(stream, token, "416 Range Not Satisfiable");
        return;
    }

    // ---- Decide strategy from size ----
    // SMALL   : buffer whole requested range in memory, single WriteAsync.
    // MEDIUM  : stream in 64 KiB chunks.
    // LARGE   : stream in 128 KiB chunks.
    FileTransferStrategy strategy = contentLength <= SmallFileThreshold
        ? FileTransferStrategy.Buffered
        : contentLength >= LargeFileThreshold
            ? FileTransferStrategy.LargeChunked
            : FileTransferStrategy.Chunked;

    int chunkSize = strategy switch
    {
        FileTransferStrategy.Buffered => (int)contentLength, // exact size; at most SmallFileThreshold
        FileTransferStrategy.LargeChunked => LargeFileChunkSize,
        _ => SmallFileChunkSize,
    };

    Console.WriteLine(
        $"[Send] {fileName} | requested {contentLength:N0} bytes " +
        $"(range {start}..{end} of {totalSize:N0}) | strategy={strategy} chunk={chunkSize:N0}");

    // ---- Response headers ----
    string disposition = ShouldForceAttachment(contentType, totalSize)
        ? $"Content-Disposition: attachment; filename=\"{fileName}\"\r\n"
        : "";

    string header;
    if (isRangeRequest)
    {
        header =
            "HTTP/1.1 206 Partial Content\r\n" +
            $"Content-Type: {contentType}\r\n" +
            disposition +
            $"Content-Length: {contentLength}\r\n" +
            $"Content-Range: bytes {start}-{end}/{totalSize}\r\n" +
            "Accept-Ranges: bytes\r\n" +
            $"ETag: {etag}\r\n" +
            "Connection: close\r\n\r\n";
    }
    else
    {
        header =
            "HTTP/1.1 200 OK\r\n" +
            $"Content-Type: {contentType}\r\n" +
            disposition +
            $"Content-Length: {totalSize}\r\n" +
            "Accept-Ranges: bytes\r\n" +
            $"ETag: {etag}\r\n" +
            "Connection: close\r\n\r\n";
    }

    await stream.WriteAsync(Encoding.ASCII.GetBytes(header), token);

    // ---- Transfer body ----
    try
    {
        switch (strategy)
        {
            case FileTransferStrategy.Buffered:
                await TransferBufferedAsync(stream, token, filePath, start, contentLength,
                    fileName, totalSize);
                break;

            case FileTransferStrategy.Chunked:
            case FileTransferStrategy.LargeChunked:
                await TransferChunkedAsync(stream, token, filePath, start, contentLength,
                    chunkSize, fileName, totalSize);
                break;
        }

        await stream.FlushAsync(token);
        Console.WriteLine($"[Send] {fileName} | DONE 100% ({contentLength:N0} bytes sent)");
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine($"[Send] {fileName} | CANCELLED");
        throw;
    }
    catch (IOException ex)
    {
        Console.WriteLine($"[Send] {fileName} | I/O error: {ex.Message}");
        throw;
    }
}

private enum FileTransferStrategy
{
    Buffered,        // small files: one shot
    Chunked,         // medium: 64 KiB
    LargeChunked,    // large:  128 KiB
}

/// <summary>
/// Small files: read the whole requested range into a byte[] and write it in one call.
/// Memory bound = contentLength (guaranteed <= SmallFileThreshold by the caller).
/// </summary>
private static async Task TransferBufferedAsync(
    NetworkStream stream,
    CancellationToken token,
    string filePath,
    long start,
    long contentLength,
    string fileName,
    long totalSize)
{
    var data = new byte[contentLength];
    using (var fs = new FileStream(
               filePath, FileMode.Open, FileAccess.Read, FileShare.Read,
               4096, FileOptions.Asynchronous | FileOptions.SequentialScan))
    {
        if (start > 0) fs.Seek(start, SeekOrigin.Begin);

        int offset = 0;
        while (offset < data.Length)
        {
            int read = await fs.ReadAsync(data.AsMemory(offset, data.Length - offset), token);
            if (read == 0) break;
            offset += read;
        }

        if (offset < data.Length)
            Array.Resize(ref data, offset);
    }

    Console.WriteLine($"[Chunk] {fileName} | bytes {start}..{start + data.Length - 1} " +
                      $"({data.Length:N3} B) | single write | progress 100.00%");

    await stream.WriteAsync(data.AsMemory(0, data.Length), token);
}

/// <summary>
/// Medium/large files: read and write in fixed chunks. Logs each chunk.
/// </summary>
private static async Task TransferChunkedAsync(
    NetworkStream stream,
    CancellationToken token,
    string filePath,
    long start,
    long contentLength,
    int chunkSize,
    string fileName,
    long totalSize)
{
    using var fs = new FileStream(
        filePath, FileMode.Open, FileAccess.Read, FileShare.Read,
        chunkSize, FileOptions.Asynchronous | FileOptions.SequentialScan);

    if (start > 0) fs.Seek(start, SeekOrigin.Begin);

    var buffer = new byte[chunkSize];
    long remaining = contentLength;
    long sent = 0;
    long currentFileOffset = start;

    while (remaining > 0)
    {
        token.ThrowIfCancellationRequested();

        int toRead = (int)Math.Min(buffer.Length, remaining);
        int read = await fs.ReadAsync(buffer.AsMemory(0, toRead), token);
        if (read == 0)
            break;

        long chunkStart = currentFileOffset;
        long chunkEnd   = currentFileOffset + read - 1;

        await stream.WriteAsync(buffer.AsMemory(0, read), token);

        sent += read;
        remaining -= read;
        currentFileOffset += read;

        double pct = contentLength > 0
            ? sent * 100.0 / contentLength
            : 100.0;

        Console.WriteLine(
            $"[Chunk] {fileName} | bytes {chunkStart}..{chunkEnd} " +
            $"({read:D} B) | sent {sent:D}/{contentLength:D} B " +
            $"| progress {pct,6:F2}%");
    }
}

    /// <summary>
    /// Parses a single-range "Range: bytes=..." header (RFC 7233 §2.1).
    /// Handles: "a-b", "a-", "-b". Returns false on malformed/multi-range.
    /// </summary>
    private static bool TryParseSingleRange(
        string headerValue,
        long totalSize,
        out long start,
        out long end,
        out bool satisfiable)
    {
        start = 0;
        end = totalSize - 1;
        satisfiable = true;

        // Expect "bytes=<spec>"
        int eq = headerValue.IndexOf('=');
        if (eq < 0) return false;

        var unit = headerValue.Substring(0, eq).Trim();
        if (!unit.Equals("bytes", StringComparison.OrdinalIgnoreCase))
            return false;

        var spec = headerValue.Substring(eq + 1).Trim();
        // We only support a single range (RFC allows multi, but it's rare and complex)
        if (spec.Contains(',')) return false;

        int dash = spec.IndexOf('-');
        if (dash < 0) return false;

        string first = spec.Substring(0, dash).Trim();
        string last = spec.Substring(dash + 1).Trim();

        // "-N"  => last N bytes
        if (first.Length == 0)
        {
            if (!long.TryParse(last, out var suffix) || suffix <= 0)
            {
                satisfiable = false;
                return true;
            }
            if (suffix > totalSize) suffix = totalSize;
            start = totalSize - suffix;
            end = totalSize - 1;
            return true;
        }

        // "N-" or "N-M"
        if (!long.TryParse(first, out start) || start < 0)
            return false;

        if (start >= totalSize)
        {
            satisfiable = false;
            return true;
        }

        if (last.Length == 0)
        {
            end = totalSize - 1;
            return true;
        }

        if (!long.TryParse(last, out end) || end < start)
            return false;

        if (end >= totalSize) end = totalSize - 1;
        return true;
    }

    // Base directory that the server is allowed to serve files from.
// Everything is resolved relative to this directory.
private static readonly string RootDirectory =
    AppContext.BaseDirectory; // or Directory.GetCurrentDirectory() if you prefer cwd

/// <summary>
/// Maps a URL path like "/bigfile.png" or "/sub/index.html" to a real file
/// under RootDirectory. Rejects traversal attempts ("..", absolute paths, etc.).
/// Returns false with a reason on failure.
/// </summary>
private static bool TryMapPathToFile(string urlPath, out string filePath, out string reason)
{
    filePath = string.Empty;
    reason = string.Empty;

    // Must start with '/'
    if (!urlPath.StartsWith('/'))
    {
        reason = "not found";
        return false;
    }

    // Percent-decode so "%20" etc. work
    string decoded;
    try { decoded = Uri.UnescapeDataString(urlPath); }
    catch { reason = "bad request"; return false; }

    // Trim leading slash and normalize separators
    var relative = decoded.TrimStart('/').Replace('\\', '/');

    // Refuse anything that could escape the root
    if (relative.Length == 0
        || relative.Contains("..")
        || Path.IsPathRooted(relative))
    {
        reason = "bad request";
        return false;
    }

    // Resolve full path and make sure it stays under RootDirectory
    string fullRoot;
    string fullPath;
    try
    {
        fullRoot = Path.GetFullPath(RootDirectory)
            .TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        fullPath = Path.GetFullPath(Path.Combine(RootDirectory, relative));
    }
    catch
    {
        reason = "bad request";
        return false;
    }

    if (!fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
    {
        reason = "bad request"; // traversal attempt
        return false;
    }

    if (!File.Exists(fullPath))
    {
        reason = "not found";
        return false;
    }

    filePath = fullPath;
    return true;
}

/// <summary>
/// Maps a file extension to a Content-Type. Add entries as needed.
/// </summary>
private static string GetContentType(string filePath)
{
    var ext = Path.GetExtension(filePath).ToLowerInvariant();
    return ext switch
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
}

/// <summary>
/// True for content types where the browser should download the file
/// rather than try to render it inline. Large binaries must be downloads;
/// small text/HTML can be viewed.
/// </summary>
private const long AttachmentSizeThreshold = 32L * 1024 * 1024; // 32 MiB

private static bool ShouldForceAttachment(string contentType, long size)
{
    // Any file big enough to hurt the browser decoder goes to the download manager
    if (size > AttachmentSizeThreshold) return true;

    return !(contentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase)
             || contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
             || contentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase)
             || contentType.StartsWith("application/javascript", StringComparison.OrdinalIgnoreCase));
}
    
    // --------------------------
    //  MINIMAL LINE READER
    // --------------------------
    // Reads CRLF-terminated lines one byte at a time from the NetworkStream
    // WITHOUT buffering beyond the line terminator. This avoids the classic
    // StreamReader read-ahead bug that swallows the body of POST requests.
    private sealed class LineReader
    {
        private readonly NetworkStream _stream;

        public LineReader(NetworkStream stream) => _stream = stream;

        public async Task<string?> ReadLineAsync(CancellationToken token)
        {
            var sb = new StringBuilder(128);
            var one = new byte[1];

            while (true)
            {
                int n = await _stream.ReadAsync(one.AsMemory(0, 1), token);
                if (n == 0)
                    return sb.Length == 0 ? null : sb.ToString();

                char c = (char)one[0];
                if (c == '\n')
                {
                    // Strip trailing '\r' if present
                    if (sb.Length > 0 && sb[^1] == '\r')
                        sb.Length--;
                    return sb.ToString();
                }
                sb.Append(c);

                // Hard cap to avoid pathological input and buffer overflow exploits
                if (sb.Length > 16 * 1024)
                    throw new IOException("Request line too long.");
            }
        }
    }
}