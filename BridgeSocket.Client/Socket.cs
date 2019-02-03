using System.Net.WebSockets;


namespace BridgeSocket.Client
{
    public class Socket : BridgeSocket.Core.Socket
    {
        public Socket(WebSocket rawSocket, int bufferSize = 4096) : base(rawSocket, null, bufferSize)
        {
            
        }
    }
}