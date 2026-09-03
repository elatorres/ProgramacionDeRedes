using System;
using System.Net.Http;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        // mandamos a contar caracteres....
        int content = await GetUrlContentLengthAsync("https://example.com");
        Console.WriteLine($"Downloaded {content} characters.");
    }

    public static async Task<int> GetUrlContentLengthAsync(string url)
    {
        // creamos un cliente http
        var client = new HttpClient();
        // Le pasamos el url para que lo baje con la función asincrónica
        Console.WriteLine("Starting download...");
        Task<string> getStringTask = client.GetStringAsync(url);
        // Mientras tanto hacemos otra cosa
        DoIndependentWork();
        // Cuando precisamos el resultado lo pedimos
        string contents = await getStringTask; // y esperamos si no está listo
        // Avisamos que terminamos
        Console.WriteLine("Got result...");
        // y devolvemos el resultado
        return contents.Length; 
    }

    static public void DoIndependentWork() // sincrónico
    {
        Console.WriteLine("Working...");
        Thread.Sleep(1000);
        Console.WriteLine("Finished working...");
    }
}