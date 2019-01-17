using System;
using System.Collections.Generic;

namespace BridgeSocket.Core
{
    /// <summary>
    /// Basic SocketManager.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SocketManager<T> : ISocketManager<T> where T : ISocket
    {
        protected readonly Dictionary<string, Namespace> Namespaces;
        public virtual event Action<T> OnConnection;
        
        public Namespace Default { get; }

        protected SocketManager()
        {
            Namespaces = new Dictionary<string, Namespace>();
            var defaultNsp = new Namespace("/");
            Default = defaultNsp;
            lock (Namespaces)
            {
                Namespaces.Add("/", defaultNsp);
            }

            defaultNsp.OnConnection += socket => { OnConnection?.Invoke((T) socket); };
        }

        public Namespace Sockets => Of("/");

        public Namespace Of(string name)
        {
            Namespace nsp = GetNamespace(name);
            if (nsp == null)
            {
                lock (Namespaces)
                {
                    nsp = new Namespace(name);
                    Namespaces.Add(name, nsp);
                }
            }
            return nsp;
        }

        protected Namespace GetNamespace(string name)
        {
            Namespace nsp = null;
            lock (Namespaces)
            {
                if (Namespaces.ContainsKey(name))
                    nsp = Namespaces[name];
            }

            return nsp;
        }
    }
}