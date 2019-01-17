using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using BridgeSocket.Core;

namespace BridgeSocket.Client
{
    public static class BridgeSocketClient
    {

        /// <summary>
        /// Connect to server.
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        public static StringSocket Connect(Uri uri, CancellationToken cancel)
        {
            var client = new ClientWebSocket();
            client.ConnectAsync(uri, cancel).Wait(CancellationToken.None);
            var socket = FromClientWebSocket(client);
            Task.Run(socket.StartListen, cancel);
            return socket;
        }
        
        /// <summary>
        /// Get <see cref="Socket"/> from a <see cref="ClientWebSocket"/> object.
        /// </summary>
        /// <param name="socket"></param>
        /// <returns></returns>
        public static StringSocket FromClientWebSocket(ClientWebSocket socket)
        {
            return new StringSocket(socket, null);
        }
    }
}