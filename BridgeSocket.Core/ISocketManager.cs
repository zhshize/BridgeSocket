using System;

namespace BridgeSocket.Core
{
    /// <summary>
    /// SocketManager manages websocket listening, namespace construction, socket construction.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ISocketManager<out T> where T : ISocket
    {
        /// <summary>
        /// When a socket connected.
        /// </summary>
        event Action<T> OnConnection;
        
        /// <summary>
        /// Get or create new namespace.
        /// </summary>
        /// <param name="name">Namespace's name.  MUST starts with "/".</param>
        /// <returns>Namespace</returns>
        Namespace Of(string name);
        
        /// <summary>
        /// Get default namespace("/").
        /// </summary>
        Namespace Default { get; }
    }
}