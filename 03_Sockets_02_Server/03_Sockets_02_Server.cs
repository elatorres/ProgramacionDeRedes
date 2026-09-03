using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Server
{
    internal class MyServer
    {
        static void Main(string[] args)
        {
            var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var endpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 2000);

            listener.Bind(endpoint);
            listener.Listen(10);

            Console.WriteLine("Servidor escuchando en puerto 20000...");

            while (true)  // loop de esperar conexiones
            {
                Socket client = listener.Accept();
                Console.WriteLine("Cliente conectado!");
                
                // nuevo hilo
                Thread t = new Thread(() => HandleClient(client));
                t.Start();
            }
        }

        static void HandleClient(Socket client)
        {
            var keepongoing = true;
            try
            {
                while (keepongoing)
                {
                    // leer largo (4 bytes)
                    byte[] buffer1 = new byte[4];
                    int received = client.Receive(buffer1, 4, SocketFlags.None);
                    if (received == 0)
                    {
                       Console.WriteLine("Cliente desconectado...");
                       keepongoing = false;
                    }
                    else
                    {
                        int length = BitConverter.ToInt32(buffer1, 0);
                        byte[] buffer2 = new byte[length];
                        received = client.Receive(buffer2, 0, length, SocketFlags.None);
                        if (received == 0)
                        {
                            Console.WriteLine("Cliente desconectado...");
                            keepongoing = false;
                        }
                        else
                        {
                            string message = Encoding.UTF8.GetString(buffer2);
                            Console.WriteLine($"Mensaje recibido: {message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error cliente: {ex.Message}");
            }
            finally
            {
                Console.WriteLine("Cliente desconectado");
                client.Close();
            }
        }
    }
}