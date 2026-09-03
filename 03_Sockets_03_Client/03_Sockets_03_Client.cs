using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Client
{
    internal class MyClient
    {
        static void Main(string[] args)
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 2000));

            Console.WriteLine("Conectado al servidor!");

            bool exit = false;

            while (!exit)
            {
                Console.WriteLine("\nSeleccione tipo:");
                Console.WriteLine("1 - String");
                Console.WriteLine("2 - Integer");
                Console.WriteLine("3 - Float");
                Console.WriteLine("4 - Exit");

                string option = Console.ReadLine();

                switch (option)
                {
                    case "1":
                        Console.Write("Ingrese string: ");
                        string str = Console.ReadLine();
                        SendString(socket, str);
                        break;

                    case "2":
                        Console.Write("Ingrese entero: ");
                        int i = int.Parse(Console.ReadLine());
                        SendInt(socket, i);
                        break;

                    case "3":
                        Console.Write("Ingrese float: ");
                        float f = float.Parse(Console.ReadLine());
                        SendFloat(socket, f);
                        break;

                    case "4":
                        exit = true;
                        break;
                }
            }

            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
        }

        // ---------- SEND HELPERS ----------

        static void SendAll(Socket s, byte[] data)
        {
            int total = 0;
            while (total < data.Length)
            {
                int sent = s.Send(data, total, data.Length - total, SocketFlags.None);
                if (sent == 0) throw new Exception("Connection lost");
                total += sent;
            }
        }

        static void SendHeader(Socket s, string type, int length)
        {
            byte[] typeBytes = Encoding.ASCII.GetBytes(type);
            byte[] lenBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(length));

            SendAll(s, typeBytes);
            SendAll(s, lenBytes);
        }

        static void SendString(Socket s, string msg)
        {
            byte[] data = Encoding.UTF8.GetBytes(msg);
            SendHeader(s, "STR", data.Length);
            SendAll(s, data);
        }

        static void SendInt(Socket s, int value)
        {
            int netInt = IPAddress.HostToNetworkOrder(value);
            byte[] data = BitConverter.GetBytes(netInt);

            SendHeader(s, "INT", data.Length);
            SendAll(s, data);
        }

        static void SendFloat(Socket s, float value)
        {
            byte[] data = BitConverter.GetBytes(value); // IEEE 754

            SendHeader(s, "FLO", data.Length);
            SendAll(s, data);
        }
    }
}