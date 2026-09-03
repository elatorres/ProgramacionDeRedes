using System;
using System.Net.Http;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Starting download...");
        string content = await DownloadPageAsync("https://example.com");
        Console.WriteLine($"Downloaded {content.Length} characters.");
    }

    static async Task<string> DownloadPageAsync(string url)
    {
        using HttpClient client = new HttpClient();
        string result = await client.GetStringAsync(url);
        return result;
    }
}