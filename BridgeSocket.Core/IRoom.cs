using System.Collections.Generic;

namespace BridgeSocket.Core
{
    
    /// <summary>
    /// Room is a group of sockets.
    /// </summary>
    public interface IRoom
    {
        /// <summary>
        /// Room's name.
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// Add a socket into this room.
        /// </summary>
        /// <param name="socket">The socket will be added to this room</param>
        void Add(ISocket socket);
        
        /// <summary>
        /// Remove a socket from this room.
        /// </summary>
        /// <param name="socket">The socket will be removed from this room</param>
        void Remove(ISocket socket);
        
        /// <summary>
        /// Broadcast event to all socket in this room.
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
    }
}