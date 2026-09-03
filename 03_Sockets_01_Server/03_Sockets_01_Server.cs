﻿using System;
using System.Net;
using System.Net.Sockets;

namespace Server
{
    internal class myServer
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("Iniciando aplicación servidor...");
            var socketServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp); //
            var localEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 20000); // Puerto va entre 0 y 65535
            socketServer.Bind(localEndpoint);
            socketServer.Listen(100); // Nuestro Socket pasa a estar en modo escucha
            Console.WriteLine("Esperando por clientes...");
            while (true)
            {
                Socket clientSocket = socketServer.Accept(); // El accept es bloqueante, espera hasta que llega una nueva conexión
                Console.WriteLine("Cliente conectado ...");
                new Thread(() => HandleClient(clientSocket)).Start(); // Lanzamos un nuevo hilo para manejar al nuevo cliente
            }

            static bool IsSocketConnected(Socket socket)
            {
                try
                {
                    return !(socket.Poll(1000, SelectMode.SelectRead) && socket.Available == 0);
                }
                catch (SocketException)
                {
                    return false;
                }
            }

            
            static void HandleClient(Socket clientSocket)
            {
                //bool clientIsConnected = true;
                // while (clientSocket.Connected)
                 while (IsSocketConnected(clientSocket))
                // while (clientIsConnected)
                {
                    Thread.Sleep(1000); // Comuncación con el cliente simulada ...
                }
                // ver con netstat -na | grep 20000
                clientSocket.Shutdown(SocketShutdown.Both);
                clientSocket.Close();

                Console.WriteLine("Cliente desconectado...");
            }
        }
    }
}