using System.Net;
using System.Net.Sockets;
using System.Text;
using Common;

namespace Server;

internal class Program
{
    private static void Main()
    {
        Console.WriteLine("Servidor iniciando...");
        // Configuramos el puerto. Creamos el EndPoint
        // Se podría inicializar con un configuration manager
        var localEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 40000); 
        // 1. Creamos nuestro tcpListener
        var tcpListener = new TcpListener(localEndpoint);
        // 2. Comenzamos a escuchar. 
        tcpListener.Start();
        // vamos a aceptar conexiones
        while (true)  // <= No hagan esto ...
        {
            // Aceptamos un nuevo tcpClient
            // Bloqueante... espera a que se conecte un cliente ...
            var tcpClient = tcpListener.AcceptTcpClient(); 
            // Lanzamos un nuevo hilo para cada cliente
            var thread = new Thread(() => handleTcpConnection(tcpClient));
            thread.Start();
        }
    }


    private static void handleTcpConnection(TcpClient client)
    {
        Console.WriteLine("Se conectó un cliente...");
        // TCPClient contiene a la clase NetworkStream que nos permite gestionar el flujo
        // de bytes en la red
        var networkStream = client.GetStream();  // sacamos el NetworkStream para afuera.
        while (true) // Ojo, no es la forma correcta de hacerlo.
        {
            // Recepción del largo del mensaje
            byte[] dataLength = new byte[Protocol.WordLength];
            // El Protocol.WordLength es el tamaño de los datos a recibir - size(int)
            int totalReceived = 0;
            while (totalReceived < Protocol.WordLength)
            {
                //  El offset es la cantidad de bytes ya recibidos en las iteraciones pasadas.
                // En esta iteración se escriben los bytes a partir de ese offset.
                var received = networkStream.Read(dataLength, totalReceived, 
                    Protocol.WordLength - totalReceived); 
                if (received == 0) // algo salio mal ...
                    // Cerrar la conexión 
                    // Lanzar excepción o lo que sea necesario para terminar el thread
                    return;
                totalReceived += received;
            }
            var length = BitConverter.ToInt32(dataLength,0);
            // Hasta acá recibimos el largo de la estructura que nos envían.
            // Recepción del mensaje
            byte[] data = new byte[length]; // obtenemos el largo del mensaje y nos preparamos para recibirlo
            totalReceived = 0;
            while(totalReceived < length) // llevamos la cuenta de lo que va llegando...
            {
                int received = networkStream.Read(data, totalReceived, length - totalReceived);
                if (received == 0)
                {
                    // Cerrar la conexión Lanzar excepción o lo que sea necesario para terminar el thread
                    networkStream.Close();
                    Console.WriteLine("Cerrando conexión...");
                    return;
                }
                totalReceived += received;
            } // inner while
            string clientData = Encoding.UTF8.GetString(data);
            Console.WriteLine($"Client says {clientData}");
        } // outer while
    } // fin handleTcpConnection
} // fin class 