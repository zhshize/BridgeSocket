using System.Collections.Generic;

namespace BridgeSocket.Core
{
    public class Room : IRoom
    {
        public List<ISocket> Sockets;
        public string Name { get; private set; }

        public Room(string name)
        {
            Sockets = new List<ISocket>();
            Name = name;
        }

        public void Add(ISocket socket)
        {
            lock (socket)
            {
                Sockets.Add(socket);
            }
        }

        public void Remove(ISocket socket)
        {
            lock (socket)
            {
                Sockets.Remove(socket);
            }
        }

        public void Broadcast<TSent, TReturned>(string name, TSent data, EventReturn<TReturned> ack = null)
        {
            lock (Sockets)
            {
                foreach (var socket in Sockets)
                {
                    socket.Emit<TSent, TReturned>(name, data, ack);
                }
            }
        }

        public List<ISocket> GetSockets()
        {
            lock (Sockets)
            {
                return Sockets;
            }
        }
    }

    public static class SocketRoomExtension
    {
        /// <summary>
        /// Join to a room.
        /// </summary>
        /// <param name="socket">The socket will be joined to</param>
        /// <param name="room">The room that the socket will joined to</param>
        public static void Join(this ISocket socket, IRoom room)
        {
            room.Add(socket);
        }

        /// <summary>
        /// Leave a room.
        /// </summary>
        /// <param name="socket">The socket will left</param>
        /// <param name="room">The room that the socket left</param>
        public static void Leave(this ISocket socket, IRoom room)
        {
            room.Remove(socket);
        }
        
        /// <summary>
        /// Join to a room.
        /// </summary>
        /// <param name="socket">The socket will be joined to</param>
        /// <param name="room">The room's name that the socket will joined to</param>
        public static void Join(this ISocket socket, string room)
        {
            socket.GetNamespace().To(room).Add(socket);
        }

        /// <summary>
        /// Leave a room.
        /// </summary>
        /// <param name="socket">The socket will left</param>
        /// <param name="room">The room's name that the socket left</param>
        public static void Leave(this ISocket socket, string room)
        {
            socket.GetNamespace().To(room).Remove(socket);
        }
        
        /// <summary>
        /// Get all rooms which this socket is in.
        /// </summary>
        /// <param name="socket">This socket</param>
        /// <returns>Room list</returns>
        public static Dictionary<string, IRoom> Rooms(this ISocket socket)
        {
            var rooms = socket.GetNamespace().GetRooms();
            var inRooms = new Dictionary<string, IRoom>();
            foreach (var (key, value) in rooms)
            {
                if (value.Sockets.Exists(socket1 => socket == socket1))
                {
                    rooms.Add(key, value);
                }
            }

            return inRooms;
        }
    }
}