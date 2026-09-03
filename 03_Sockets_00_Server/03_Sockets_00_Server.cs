using System;
using System.Net;
using System.Net.Sockets;

namespace Server
{
    internal class myServer
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("Comenzando la aplicación servidor...");
            // Configuramos el protocolo de red Internet/TCP/IP
            var socketServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            // Configuramos la dirección del servidor
            //Puerto va entre 0 y 65535 preferiblemente por encima de 1024
            var localEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 20000);
            // Establecemos la dirección    
            socketServer.Bind(localEndpoint);
            // iniciamos la escucha del puerto por parte del servidor
            socketServer.Listen(1); // Nuestro Socket pasa a estar en modo escucha
            // Explicar socket.Listen(1) en Windows vs Linux
            // Esperamos a que un cliente haga la solicitud de conexión
            var socketClient = socketServer.Accept(); // Bloqueante
            Console.WriteLine("Acepté un nuevo pedido de conexión");
            Console.ReadLine();
        }
    }
}
// y termino
// ¿Qué pasa si varios clientes se conectan?
// ¿Qué pasa con el segundo cliente si IPEndPoint(127.0.0.1, 15000) en los clientes?
// ¿Qué pasa con el tercer cliente si socketServer.Listen(1);
