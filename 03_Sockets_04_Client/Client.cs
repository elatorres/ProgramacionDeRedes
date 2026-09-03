﻿using System;
using Client;
using Common;
using Common.Interfaces;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Client
{
    class Client
    {
        static readonly ISettingsManager SettingsMgr = new SettingsManager();
        static void Main(string[] args)
        {
            // el cliente tambien debe tener archivo de configuración
            
            Console.WriteLine("Creando Socket Client");
            Socket cliente = new Socket(
                AddressFamily.InterNetwork, // Indica que vamos a usar IP
                SocketType.Stream, // Socket orientado a conexión
                ProtocolType.Tcp); // Usamos TCP como protocolo de transporte
            
            // La ip y puerto que el cliente va a usar, si la ip del cliente y del server es la misma, el puerto debe ser distinto
            var localEndpoint = new IPEndPoint(IPAddress.Parse(SettingsMgr.ReadSetting(ClientConfig.ClientIpConfigKey)), 0);
            
            // Le digo al cliente que ip y puerto local usar (no es necesario hacer el Bind en el cliente)
            cliente.Bind(localEndpoint);
            
            // Especifico la ip y puerto del servidor al cual el cliente se va a conectar
            // El cliente debe saber la ip y puerto del servidor para poder conectarse
            var remoteEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), int.Parse(SettingsMgr.ReadSetting(ClientConfig.SeverPortConfigKey)));
            try
            {
                cliente.Connect(remoteEndpoint);
                
                while (true) // No se debería hacer un while (true)
                {
                    Console.WriteLine("Ingrese un mensaje: ");
                    string message = Console.ReadLine();
                    if (message.Length > 0)
                    {
                        byte[] messageInBytes = Encoding.UTF8.GetBytes(message); // Convierto el string en una array de bytes
                        cliente.Send(messageInBytes); // Mando la array de bytes a travéz del Socket 
                    }
                    else
                    {
                        break; // La mejor practica sería poner una variable booleana que empiece en false y aca pasarla a true
                    }
                }
                cliente.Shutdown(SocketShutdown.Both);
                cliente.Close();
            }
            
            catch (SocketException s)
            {
                Console.WriteLine($"Error: {s.ErrorCode}, Error Code: {s.SocketErrorCode}");
                // SocketErrorCodes: https://docs.microsoft.com/en-us/dotnet/api/system.net.sockets.socketerror?view=net-6.0
            }
        }
    }
}