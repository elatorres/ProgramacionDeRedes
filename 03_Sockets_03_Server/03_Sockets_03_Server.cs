using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Server
{
    internal class MyServer
    {
        static void Main(string[] args)
        {
            var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 2000));
            listener.Listen(10);

            Console.WriteLine("Servidor iniciado...");

            while (true)
            {
                var client = listener.Accept();
                Console.WriteLine("Cliente conectado");

                new Thread(() => HandleClient(client)).Start();
            }
        }

        static void HandleClient(Socket client)
        {
            try
            {
                while (true)
                {
                    // 1. Read type (3 bytes)
                    byte[] typeBuf = ReceiveAll(client, 3);
                    if (typeBuf == null) break;

                    string type = Encoding.ASCII.GetString(typeBuf);

                    // 2. Read length
                    byte[] lenBuf = ReceiveAll(client, 4);
                    if (lenBuf == null) break;

                    int length = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(lenBuf, 0));

                    // 3. Read data
                    byte[] data = ReceiveAll(client, length);
                    if (data == null) break;

                    // 4. Interpret
                    switch (type)
                    {
                        case "STR":
                            string str = Encoding.UTF8.GetString(data);
                            Console.WriteLine($"[STRING] {str}");
                            break;

                        case "INT":
                            int i = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(data, 0));
                            Console.WriteLine($"[INT] {i}");
                            break;

                        case "FLO":
                            float f = BitConverter.ToSingle(data, 0);
                            Console.WriteLine($"[FLOAT] {f}");
                            break;

                        default:
                            Console.WriteLine($"Tipo desconocido: {type}");
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
            finally
            {
                Console.WriteLine("Cliente desconectado");
                client.Close();
            }
        }

        static byte[] ReceiveAll(Socket s, int size)
        {
            byte[] buffer = new byte[size];
            int total = 0;

            while (total < size)
            {
                int received = s.Receive(buffer, total, size - total, SocketFlags.None);
                if (received == 0) return null;
                total += received;
            }

            return buffer;
        }
    }
}