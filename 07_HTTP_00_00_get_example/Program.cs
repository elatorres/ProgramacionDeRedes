using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class Program
{
    // This program demonstrates the GET verb in HTTP.
    // It requests a web page and shows the http request and the http response in console. 
    // We are not using an httpclient. We are reading directly from the network through TcpClient.
    static async Task Main(string[] args)
    {
        Console.WriteLine("Press any key to start the HTTP request...");
        Console.ReadKey(true);
        
        // solicitamos la página http://www.example.com
        string host = "www.example.com";
        int port = 80; //http port=80

        try
        {
            await MakeHttpRequestAsync(host, port);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nError: {ex.Message}");
        }

        Console.WriteLine("\nTask complete. Press any key to exit.");
        Console.ReadKey();
    }

    static async Task MakeHttpRequestAsync(string host, int port)
    {
        using (TcpClient client = new TcpClient())
        {
            // Conectarse al servidor asincrónicamente
            await client.ConnectAsync(host, port);
            
            // extraemos en network stream para enviar la solicitud
            using (NetworkStream stream = client.GetStream())
            {
                // Construimos el texto de la solicitud HTTP GET. Usamos HTTP/1.1
                // e incluimos el encabezado obligatorio del 'Host' que lo pasamos
                // por parámetro.
                string httpRequest = $"GET / HTTP/1.1\r\n" +
                                     $"Host: {host}\r\n" +
                                     $"Connection: close\r\n" +
                                     $"\r\n";
                
                // Lo mandamos por TCP/IP como bytes
                byte[] requestBytes = Encoding.ASCII.GetBytes(httpRequest);

                // Mostramos en la consola lo que vamos a mandar
                Console.WriteLine("--- SENT MESSAGE ---");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write(httpRequest);
                Console.ResetColor();

                // Y enviamos el mensaje 
                await stream.WriteAsync(requestBytes, 0, requestBytes.Length);

                // Leemos la respuesta asincrónicamente
                byte[] buffer = new byte[4096];
                // Usamos  System.Text.StringBuilder para crear una cadena de largo 0
                StringBuilder responseBuilder = new StringBuilder();
                int bytesRead; // Los bytes que vamos leyendo los guardamos acá
                
                // Indicamos lo que vamos a recibir...
                Console.WriteLine("\n--- RECEIVED RESPONSE ---");
                Console.ForegroundColor = ConsoleColor.Yellow;
                
                // Y vamos recibiendo (por las dudas, no sabemos el tamaño de lo que vamos
                // a recibir) en un while contando los bytes recibidos.
                // Deberíamos cortar si el largo es demasiado largo (Buffer Overflow Exploit?) 
                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    string part = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    responseBuilder.Append(part);
                    
                    // Y vamos imprimientdo por partes a medida que recibimos del stream ¿?
                    Console.Write(part); 
                }
                Console.ResetColor();
            } // using NetworkStream End
        } // using TcpClient End
    } // MakeHttpRequestAsync End
}
