using System;
using System.Collections.Generic;

namespace BridgeSocket.Core
{
    public class Namespace : INamespace
    {
        public readonly List<ISocket> Sockets;
        public readonly Dictionary<string, Room> Rooms;

        public string Name { get; private set; }
        public event SocketHandler OnConnection;

        public Namespace(string name)
        {
            Sockets = new List<ISocket>();
            Rooms = new Dictionary<string, Room>();
            Name = name;
        }

        public void AddSocket(ISocket socket)
        {
            socket.OnDisconnect += (status, reason) =>
            {
                lock (Sockets)
                {
                    Sockets?.Remove(socket);
                }
            };
            lock (Sockets)
            {
                Sockets.Add(socket);
            }
            OnConnection?.Invoke(socket);
        }

        public void Broadcast<TSent, TReturned>(string name, TSent data, EventReturn<TReturned> ack = null)
        {
            lock (Sockets)
            {
                foreach (var socket in Sockets)
                {
                    socket.Emit(name, data, ack);
                }
            }
        }
        
        public IRoom To(string name)
        {
            Room room = GetRoom(name);
            if (room == null)
            {
                lock (Rooms)
                {
                    room = new Room(name);
                    Rooms.Add(name, room);
                }
            }
            return room;
        }

        public List<ISocket> GetSockets()
        {
            lock (Sockets)
            {
                return Sockets;
            }
        }
        
        public Dictionary<string, Room> GetRooms()
        {
            lock (Rooms)
            {
                return Rooms;
            }
        }
        
        protected Room GetRoom(string name)
        {
            Room room = null;
            lock (Rooms)
            {
                if (Rooms.ContainsKey(name))
                    room = Rooms[name];
            }

            return room;
        }
    }
}