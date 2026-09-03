using Common;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

internal class Server
{
    private static void Main(string[] args)
    {
        Console.WriteLine("Creando Socket Server");
        // declaro el socket 
        var server = new Socket(
            AddressFamily.InterNetwork,
            SocketType.Stream,
            ProtocolType.Tcp);
        // Declaro el EndPoint
        var localEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 30000);
        // el bind al endpoint local
        server.Bind(localEndpoint);
        // Establezco el backlog del socket, a tres lugares
        var backlog = 3;
        // El socket comienza a escuchar solicitudes
        server.Listen(backlog);
        // mientras sigamos así ....
        while (true)
        {
            // Acepto conexiones de clientes
            var cliente = server.Accept(); // Es bloqueante
            // Creo un manejador de cliente y le paso el socket de la conexión 
            var manejarCliente = new Thread(() => HandleClient(cliente));
            manejarCliente.Start();
        }
    }

    private static void HandleClient(Socket cliente)
    {
        // Acepte un cliente y estoy conectado 
        Console.WriteLine("Acepte un nuevo cliente");
        // Creo un NetworkHelper para enviar y recibir datos
        var networkHelper = new NetworkHelper(cliente);

        var conectado = true;
        while (conectado)
        {
            // Parte 1: Recibo el LARGO del nombre del archivo
            var largoDelNombreDelArchivoEnBytes = networkHelper.Receive(Protocol.LargoFijo);
            // convertir de bytes a int
            var largoDelNombreDelArchivo = BitConverter.ToInt32(largoDelNombreDelArchivoEnBytes, 0);
            // Parte 2: Recibo el nombre del archivo
            var nombreDelArchivoBytes = networkHelper.Receive(largoDelNombreDelArchivo);
            var nombreDelArchivo = Encoding.UTF8.GetString(nombreDelArchivoBytes);
            // Parte 3: Recibo el largo del archivo
            var largoDelArchivoEnBytes = networkHelper.Receive(Protocol.LargoFijoArchivo);
            var largoDelArchivo = BitConverter.ToInt64(largoDelArchivoEnBytes, 0);
            // Parte 4: Recibo el archivo (por partes)
            // primero calculo la cantidad de partes que voy a recibir: 
            var cantidadDePartes = Protocol.CalcularCantidadDePartes(largoDelArchivo);
            long currentPart = 1; // la parte del archivo que estoy procesando ahora mismo
            long offset = 0; // el byte donde voy a empezar a leer
            var fileStreamHelper = new FileStreamHelper();
            while (offset < largoDelArchivo)
            {
                // recibo partes
                byte[] data;
                var esUltimaParte = currentPart == cantidadDePartes;
                // opcion 1: recibo una parte que NO es la ultima
                if (!esUltimaParte)
                {
                    Console.WriteLine($"Recibiendo una parte de tamaño {Protocol.MaxFileSizePart}");
                    data = networkHelper.Receive(Protocol.MaxFileSizePart);
                    offset += Protocol.MaxFileSizePart;
                } // opcion 2: recibo la ultima parte 
                else
                {
                    var tamanoUltimaParte = (int)(largoDelArchivo - offset);
                    Console.WriteLine($"Recibiendo una parte de tamaño {tamanoUltimaParte}");
                    data = networkHelper.Receive(tamanoUltimaParte);
                    offset += tamanoUltimaParte;
                }

                // escribo la data a disco 
                fileStreamHelper.Write(nombreDelArchivo, data);
                currentPart++;
            }

            Console.WriteLine("Termine de recibir archivo {0}!", nombreDelArchivo);
        }

        Console.WriteLine("Cerrando conexión con cliente...");
        cliente.Shutdown(SocketShutdown.Both);
        cliente.Close();
    }
}