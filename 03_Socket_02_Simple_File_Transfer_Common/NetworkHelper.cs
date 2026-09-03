using System.Net.Sockets;

namespace Common // Lo voy a poder usar tanto en el cliente como en el servidor
{
    public class NetworkHelper
    {
        private readonly Socket _socket; // solo se puede inicializar en el constructor
        public NetworkHelper(Socket socket)
        {
            _socket = socket;
        }

        public void Send(byte[] data)
        {
            int offset = 0;
            int size = data.Length;
            while (offset < size)
            {
                int sent = _socket.Send(data, offset, data.Length - offset, SocketFlags.None);
                if (sent == 0)
                {
                    throw new SocketException();
                }
                offset += sent;
            }
        }

        public byte[] Receive(int dataLength)
        {
            byte[] response = new byte[dataLength];
            int offset = 0;
            while (offset < dataLength)
            {
                int received = _socket.Receive(response, offset, dataLength - offset, SocketFlags.None);
                if (received == 0)
                {
                    throw new SocketException();
                }
                offset += received;
            }
            return response;
        }
    }
}