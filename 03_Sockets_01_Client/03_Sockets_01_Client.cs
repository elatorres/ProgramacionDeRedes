using System.Net;
using System.Net.Sockets;

namespace Client
{
    internal class myClient
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("Starting Client Application..");
            var socketClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var localEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 0); // <<==
            var remoteEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 20000);
            socketClient.Bind(localEndpoint);
            Console.WriteLine("Iniciando Cliente...");
            Console.WriteLine("Connectándose al Servidor...");
            socketClient.Connect(remoteEndpoint);
            Console.WriteLine("Connectado al servidor!!!!");
            Console.WriteLine("Presionar [Enter] para salir");
            Console.ReadLine();
            Console.WriteLine("Se cerrará la conexión...");
            socketClient.Shutdown(SocketShutdown.Both);
            socketClient.Close();
            socketClient.Dispose();
        }
    }
}