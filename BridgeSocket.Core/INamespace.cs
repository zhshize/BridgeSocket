using System.Collections.Generic;

namespace BridgeSocket.Core
{
    public delegate void SocketHandler(ISocket socket);
    
    /// <summary>
    /// Namespace is a set of rooms and sockets which are constructed from same URL endpoint.
    /// </summary>
    public interface INamespace
    {
        /// <summary>
        /// When a socket connected.
        /// </summary>
        event SocketHandler OnConnection;

        /// <summary>
        /// Get <see cref="IRoom"/> instance.
        /// </summary>
        /// <param name="name">Room's name</param>
        /// <returns><see cref="IRoom"/></returns>
        IRoom To(string name);
        
        /// <summary>
        /// Namespace's name, always start with "/".
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// Add a socket into this namespace.
        /// You should NOT call this function except you want to add new feature.
        /// </summary>
        /// <param name="socket"></param>
        void AddSocket(ISocket socket);
        
        /// <summary>
        /// Broadcast event to all socket in this namespace.
        /// </summary>
        /// <param name="name">Event name,  null string is invalid.</param>
        /// <param name="data">Data</param>
        /// <param name="ack">If endpoint responds a message, ack will be invoke with response data.</param>
        /// <typeparam name="TSent">Type of data you want to send.</typeparam>
        /// <typeparam name="TReturned">Type of response data from endpoint.</typeparam>
        void Broadcast<TSent, TReturned>(string name, TSent data, EventReturn<TReturned> ack = null);
        
        /// <summary>
        /// Get all sockets in this namespace.
        /// </summary>
        /// <returns>All sockets in this namespace</returns>
        List<ISocket> GetSockets();
        
        /// <summary>
        /// Get all rooms in this namespace.
        /// </summary>
        /// <returns>All rooms in this namespace</returns>
        Dictionary<string, Room> GetRooms();
    }
}