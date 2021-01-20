using System.Net.Sockets;

namespace Chat
{
    class SocketAsyncState
    {
        public Socket socket;
        public const int bufferSize = 64;
        public byte[] buffer = new byte[bufferSize];

        public SocketAsyncState(Socket socket)
        {
            this.socket = socket;
        }
    }
}
