using System.Net;
using System.Net.Sockets;
using System.Text;
using Common;
using Common.Interfaces;

namespace Server;

internal class Program
{
    private static readonly ISettingsManager SettingsMgr = new SettingsManager();

    private static void Main(string[] args)
    {
        Console.WriteLine("Creando Socket Server");
        Console.WriteLine("Server is starting..." + SettingsMgr.ReadSetting(ServerConfig.ServerIpConfigKey));
        Console.WriteLine("Server is starting..." + SettingsMgr.ReadSetting(ServerConfig.SeverPortConfigKey));

        var server = new Socket(
            AddressFamily.InterNetwork, // Indica que vamos a usar IP
            SocketType.Stream, // Socket orientado a conexión 
            ProtocolType.Tcp); // Usamos TCP como protocolo de transporte 

        // Para usar nuestra ip podemos usar ipconfig en windows o ifconfig en Mac. Ej. 192.168.0.121
        // Para el puerto podemos elegir cualquiera disponible (netstat -an)

        // Rango validos de puertos -> 1 - 65535, PERO del 1 al 1024 generalmente no los usamos

        var localEndpoint = new IPEndPoint(
            IPAddress.Parse(SettingsMgr.ReadSetting(ServerConfig.ServerIpConfigKey)),
            int.Parse(SettingsMgr.ReadSetting(ServerConfig.SeverPortConfigKey)));
        server.Bind(localEndpoint); // Le digo al server cual es el endpoint local a utilizar

        Console.WriteLine("Escuchando por nuevas conexiones");
        server.Listen(10); //Pongo el socket server en modo escucha.  10 es un número arbritario que especifcia el tamaño del backlog


        // Acepta una nueva conexión, se libera un especio en el backlog del .Listen
        try
        {
            var acceptedConnection = server.Accept();

            // Manejo la conexión...
            Console.WriteLine("Acepte una conexion nueva!");

            var bytesRecibidos = 1;

            while (bytesRecibidos > 0)
            {
                var buffer = new byte[1024];
                bytesRecibidos = acceptedConnection.Receive(buffer);

                if (bytesRecibidos > 0)
                {
                    var message = Encoding.UTF8.GetString(buffer);
                    Console.WriteLine($"Mensaje recibido: {message}");
                }
                else
                {
                    Console.WriteLine("Conexión cerrada....");
                }
            }

            acceptedConnection.Shutdown(SocketShutdown.Both);
            acceptedConnection.Close();
        }
        catch (SocketException s)
        {
            Console.WriteLine($"Error: {s.ErrorCode}, Error Code: {s.SocketErrorCode}");
        }
    }
}