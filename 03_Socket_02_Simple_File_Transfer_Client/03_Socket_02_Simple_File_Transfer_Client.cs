using Common;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

internal class Client
{
    private static void Main(string[] args)
    {
        Console.WriteLine("Arrancando cliente...");
        // declaro el socket
        var socketCliente = new Socket(
            AddressFamily.InterNetwork,
            SocketType.Stream,
            ProtocolType.Tcp);
        var localEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 0);
        // el bind al endpoint local
        socketCliente.Bind(localEndpoint);
        var remoteEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 30000);
        // me conecto al server: 
        try
        {
            socketCliente.Connect(remoteEndpoint);
            Console.WriteLine("Me conecté al servidor");
            // Agrego un NetworkHelper
            var networkHelper = new NetworkHelper(socketCliente);
            // estoy conectado!!
            var conectado = true;
            while (conectado)
            {
                // mientras estoy conectado
                try
                {
                    // Pido el archivo al usuario: 
                    Console.WriteLine("Ingrese ruta del archivo (incluyendo el nombre y extension): ");
                    var filePath = Console.ReadLine();
                    // guardo en nombre del archivo
                    var nombreDelArchivo = new FileInfo(filePath).Name;
                    var nombreDelArchivoEnBytes = Encoding.UTF8.GetBytes(nombreDelArchivo);
                    // Parte 1: Envio el LARGO del nombre del archivo
                    var largoDelNombreDelArchivo = nombreDelArchivoEnBytes.Length;
                    var largoDelNombreDelArchivoEnBytes = BitConverter.GetBytes(largoDelNombreDelArchivo);
                    networkHelper.Send(largoDelNombreDelArchivoEnBytes);
                    // Parte 2: Envio el nombre del archivo
                    networkHelper.Send(nombreDelArchivoEnBytes);
                    // Parte 3: Envio el largo del archivo
                    var largoDelArchivo = new FileInfo(filePath).Length;
                    var largoDelArchivoEnBytes = BitConverter.GetBytes(largoDelArchivo);
                    networkHelper.Send(largoDelArchivoEnBytes);
                    // Parte 4: Envio el archivo (por partes)
                    // primero calculo la cantidad de partes que voy a recibir: 
                    var cantidadDePartes = Protocol.CalcularCantidadDePartes(largoDelArchivo);
                    long currentPart = 1; // la parte del archivo que estoy procesando ahora mismo
                    long offset = 0; // el byte donde voy a empezar a leer
                    var fileStreamHelper = new FileStreamHelper();
                    while (offset < largoDelArchivo)
                    {
                        // envio partes
                        byte[] data;
                        var esUltimaParte = currentPart == cantidadDePartes;
                        // opcion 1: envio una parte que NO es la ultima
                        if (!esUltimaParte)
                        {
                            Console.WriteLine($"Enviando parte de tamaño: {Protocol.MaxFileSizePart}");
                            data = fileStreamHelper.Read(filePath, offset, Protocol.MaxFileSizePart);
                            offset += Protocol.MaxFileSizePart;
                        }
                        // opcion 2: envio la ultima parte
                        else
                        {
                            var lastPartSize = (int)(largoDelArchivo - offset);
                            Console.WriteLine($"Enviando parte de tamaño: {lastPartSize}");
                            data = fileStreamHelper.Read(filePath, offset, lastPartSize);
                            offset += lastPartSize;
                        }

                        // envio la data
                        networkHelper.Send(data);
                        // Llevo la cuenta de las partes
                        currentPart++;
                    }

                    Console.WriteLine("Termine de enviar el archivo!");
                }
                catch (SocketException)
                {
                    Console.WriteLine("Se desconecto el cliente");
                    conectado = false;
                }
            }
            // cierro las conexiones
            socketCliente.Shutdown(SocketShutdown.Both);
            socketCliente.Close();
        }
        catch (SocketException e)
        {
            Console.WriteLine($"Excepcion: {e.Message}, Codigo: {e.ErrorCode}");
        }

        Console.WriteLine("Cerrando cliente... Envie enter para cerrar consola");
        Console.ReadLine();
    }
}