using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Client
{
    internal class myClient
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("Comienza el CLiente...");
            var socketClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var localEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 0); 
            var remoteEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 2000);
            socketClient.Bind(localEndpoint);
            socketClient.Connect(remoteEndpoint);
            Console.WriteLine("Connectado al servidor!!!!");
            
            var option = "";
            var exit = false;
            while (!exit)
            {
                Console.WriteLine("Escriba un mensaje para el servidor.");
                Console.WriteLine("Escriba 'exit' para salir.");
                option = Console.ReadLine();
                if (option.Equals("exit")) // si exit terminamos
                {
                    exit = true;
                }
                else
                {
                    
                    byte[] data = Encoding.UTF8.GetBytes(option);
                    // Obtenemos el largo de los datos a enviar
                    byte[] dataLength = BitConverter.GetBytes(data.Length);
                    // Enviamos el largo del mensaje
                    int sent1 = socketClient.Send(dataLength, 4, SocketFlags.None);
                    if (sent1 == 0)
                    {
                        Console.WriteLine("Se perdió la comunicación");
                        exit = true;;
                    }
                    else
                    {
                        // enviamos el mensaje
                        int sent2 = socketClient.Send(data, data.Length, SocketFlags.None);
                        if (sent2 == 0)
                        {
                            Console.WriteLine("Se perdió la comunicación");
                            exit = true;;
                        }
                        Console.WriteLine("\nCantidad de caracteres: {0} - Cantidad de bytes: {1}",option.Length, data.Length);
                    }
                } // if
            } // while
            Console.WriteLine("Presionar [Enter] para salir");
            Console.ReadLine();
            
            Console.WriteLine("Se cerrará la conexión...");
            socketClient.Shutdown(SocketShutdown.Both);
            socketClient.Close();
            socketClient.Dispose();
        }
    }
}