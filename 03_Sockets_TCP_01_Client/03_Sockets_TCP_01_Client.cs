using System.Net;
using System.Net.Sockets;
using System.Text;
using Common;

namespace Client;

internal class Program
{
    private static void Main()
    {
        Console.WriteLine("Cliente iniciando...");
        var localEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 0); //
        var remoteEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 40000); //
        var tcpClient = new TcpClient(localEndpoint);
        Console.WriteLine("Tratando de conectarse al servidor...");
        tcpClient.Connect(remoteEndpoint); //
        Console.WriteLine("Conectado al servidor...");
        NetworkStream networkStream = tcpClient.GetStream(); //
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
                // Obtenemos los datos a enviar
                byte[] data = Encoding.UTF8.GetBytes(option);
                // Obtenemos el largo de los datos a enviar
                byte[] dataLength = BitConverter.GetBytes(data.Length);
                // Enviamos el largo del mensaje
                networkStream.Write(dataLength, 0, Protocol.WordLength); //
                // Enviamos el mensaje
                networkStream.Write(data, 0, data.Length);
            } // if
        } // while
        networkStream.Close(); // Conexión
        tcpClient.Close(); // Cerrramos TcpClient
    } // Main
} // Program class