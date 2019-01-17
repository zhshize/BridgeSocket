using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace BridgeSocket.Core
{
    public delegate void EventReturn(byte[] value);

    public delegate void EventAction(byte[] data, EventReturn respond);

    public delegate void EventReturn<in TReturn>(TReturn value);

    public delegate void EventAction<in THandle, out TReturn>(THandle data, EventReturn<TReturn> respond);

    public interface ISocket
    {
        /// <summary>
        /// The <see cref="WebSocket">WebSocket</see> object.
        /// </summary>
        WebSocket RawSocket { get; }

        /// <summary>
        /// Will be invoke when server or client receive close request.
        /// </summary>
        event Action<WebSocketCloseStatus, string> OnDisconnecting;

        /// <summary>
        /// Will be invoke when socket closed.
        /// </summary>
        event Action<WebSocketCloseStatus, string> OnDisconnect;

        /// <summary>
        /// Get namespace object which this socket is in.
        /// </summary>
        /// <returns></returns>
        INamespace GetNamespace();

        /// <summary>
        /// Get identity of this socket.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Start listen to message, You can call this function ONLY ONCE.
        /// </summary>
        /// <returns>Task object in promise style</returns>
        Task StartListen();

        /// <summary>
        /// Emit an event to endpoint.
        /// </summary>
        /// <param name="name">Event name,  null string is invalid.</param>
        /// <param name="data">Data</param>
        /// <param name="ack">If endpoint responds a message, ack will be invoke with response data.</param>
        /// <typeparam name="TSent">Type of data you want to send.</typeparam>
        /// <typeparam name="TReturned">Type of response data from endpoint.</typeparam>
        void Emit<TSent, TReturned>(string name, TSent data, EventReturn<TReturned> ack = null);

        /// <summary>
        /// Handle a event.
        /// </summary>
        /// <param name="name">Event name</param>
        /// <param name="callBack">Will be invoke with data from endpoint</param>
        /// <typeparam name="TReceived">Type of data received.</typeparam>
        /// <typeparam name="TReturned">Type of data you will returned.</typeparam>
        void On<TReceived, TReturned>(string name, EventAction<TReceived, TReturned> callBack);

        /// <summary>
        /// Remove event handler.
        /// </summary>
        /// <param name="name">Event name</param>
        /// <param name="callBack">The callback you registered before.</param>
        void RemoveHandler(string name, EventAction callBack);

        /// <summary>
        /// Remove all event handler by event name.
        /// </summary>
        /// <param name="name">Event name</param>
        void RemoveAllHandler(string name);

        /// <summary>
        /// Get the list of event names.
        /// </summary>
        /// <returns>The list of event names</returns>
        List<string> EventNames();

        /// <summary>
        /// Disconnect and close connection.
        /// </summary>
        void Disconnect(WebSocketCloseStatus closeStatus = WebSocketCloseStatus.NormalClosure,
            string statusDescription = "");
    }
}