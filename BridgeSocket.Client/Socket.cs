using System.Net.WebSockets;
using BridgeSocket.Core;


namespace BridgeSocket.Client
{
    public class Socket : BridgeSocket.Core.Socket
    {
        public Socket(WebSocket rawSocket, int bufferSize = 4096) : base(rawSocket, null, bufferSize)
        {
            
        }
    }
}