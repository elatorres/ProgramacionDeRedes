using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class AsyncTcpClient
{
    static async Task Main()
    {
        try
        {
            // Este es mi TcpClient
            using TcpClient client = new TcpClient();
            // localhost y puerto 9000
            await client.ConnectAsync("127.0.0.1", 9000);
            // ya estoy conectado. Debería atrapar la excepción acá si no hay servidor que escuche...
            Console.WriteLine("Connected to server. Type messages or 'exit' to quit.");
            // extraigo en network stream para la conexión.
            using NetworkStream stream = client.GetStream();
            // Escribir y leer en el stream.
            while (true)
            {
                //leo de la consola un mensaje
                string? message = Console.ReadLine();
                // si es exit salimos
                if (message?.ToLower() == "exit")
                    break;
                // si no es exit, enviamos el mensaje al servidor.
                // preparamos los buffers
                byte[] buffer = Encoding.UTF8.GetBytes(message);
                byte[] length = BitConverter.GetBytes(buffer.Length);
                // y los mandamos
                await stream.WriteAsync(length);
                await stream.WriteAsync(buffer);
                // leemos la respuesta del servidor.
                // primero el largo
                byte[] lengthBuffer = new byte[4];
                int read = await stream.ReadAsync(lengthBuffer);
                int msgLength = BitConverter.ToInt32(lengthBuffer, 0);
                // y luego la respuesta
                byte[] responseBuffer = new byte[msgLength];
                read = await stream.ReadAsync(responseBuffer, 0, msgLength);
                string echo = Encoding.UTF8.GetString(responseBuffer, 0, read);
                // y la escribimos a la consola
                Console.WriteLine($"Echo: {echo}");
            }
            client.Close();
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"❌ Connection error: {ex.Message}");
        }
        catch (IOException ex)
        {
            Console.WriteLine($"❌ Network stream error: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Unexpected error: {ex.Message}");
        }
        finally
        {
            // si se terminó... se terminó...
            Console.WriteLine("🔚 Client terminated. Press any key to exit.");
            Console.ReadKey();
        }
    }
}
