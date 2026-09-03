using System;
using System.Net;
using System.Net.Sockets;

namespace Client
{
    internal class myClient
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("Comenzando la aplicación cliente...");
            // configurar el protocolo Internet/TCP/IP
            var socketClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            // Desde donde nos conectamos ...
            var localEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 15000); // cambiar a 0);
            // Hacia donde esperamos que esté el servidor ...
            var remoteEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 20000);
            // hacemos los binds a la interface local
            socketClient.Bind(localEndpoint);
            // Nos conectamos con el servidor (remoto)
            socketClient.Connect(remoteEndpoint);
            // Acá ya estamos conectados...
            Console.WriteLine("Conectado al servidor!!!!");
            Console.WriteLine("Presione [Enter] para terminar...");
            Console.ReadLine(); // esperamos el Enter ....
            Console.WriteLine("Cerrando la conexión...");
            // Indica en que sentido quiero cerrar la conexion
            socketClient.Shutdown(SocketShutdown.Both);
            // Terminamos...
            socketClient.Close(); // Fin
        }
    }
}