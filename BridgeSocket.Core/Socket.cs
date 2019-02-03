using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BridgeSocket.Core.Message;
using MessagePack;

namespace BridgeSocket.Core
{
    /// <summary>
    /// A basic socket send/receive with byte[].
    /// You can extend it to add some custom feature.
    /// </summary>
    public class Socket : ISocket
    {
        public virtual WebSocket RawSocket { get; set; }
        public string Id { get; private set; }
        public event Action<WebSocketCloseStatus, string> OnDisconnecting;
        public event Action<WebSocketCloseStatus, string> OnDisconnect;

        protected readonly Dictionary<string, EventAction> EventHandlers;
        protected readonly Dictionary<string, EventReturn> EventResponses;
        protected readonly ConcurrentQueue<byte[]> SendQueue;

        private const string RespondEventName = null;
        private readonly int _bufferSize;

        private bool _reading;
        private INamespace _nsp;
        private int _callBackIdRandomLength = 8;

        private readonly CancellationTokenSource _cancellation = new CancellationTokenSource();

        /// <summary>
        /// Construct a socket.
        /// </summary>
        /// <param name="rawSocket">The <see cref="WebSocket">WebSocket</see> object.</param>
        /// <param name="from">The <see cref="INamespace">namespace</see> object which construct this socket.</param>
        /// <param name="bufferSize">The receiving buffer size.</param>
        public Socket(WebSocket rawSocket, INamespace from, int bufferSize = 1024 * 4)
        {
            RawSocket = rawSocket;
            _bufferSize = bufferSize;
            Id = RandomString(16);
            EventHandlers = new Dictionary<string, EventAction>();
            EventResponses = new Dictionary<string, EventReturn>();
            _reading = false;
            SendQueue = new ConcurrentQueue<byte[]>();
            _nsp = from;
        }

        public INamespace GetNamespace()
        {
            return _nsp;
        }

        protected virtual void HandleMessage(Stream s)
        {
            EventMessage eventMessage;
            try
            {
                eventMessage = MessagePackSerializer.Deserialize<EventMessage>(s);
            }
            catch (Exception)
            {
                return;
            }

            // If is callback
            if (eventMessage.Name == RespondEventName)
            {
                EventReturn respond;
                lock (EventResponses)
                {
                    if (!EventResponses.ContainsKey(eventMessage.CallBackId))
                        return;
                    respond = EventResponses[eventMessage.CallBackId];
                }

                respond?.Invoke(eventMessage.Data);
            }
            else
            {
                EventAction handler;
                lock (EventHandlers)
                {
                    if (!EventHandlers.ContainsKey(eventMessage.Name))
                        return;
                    handler = EventHandlers[eventMessage.Name];
                }

                handler?.Invoke(eventMessage.Data, Respond(eventMessage.CallBackId));
            }
        }

        protected EventReturn Respond(string callBackId)
        {
            return s =>
            {
                var em = new EventMessage {Name = RespondEventName, Data = s, CallBackId = callBackId};
                SendAsync(em.ToMessagePack());
            };
        }

        /// <summary>
        /// Emit a event with byte array data.
        /// </summary>
        /// <param name="name">Event name</param>
        /// <param name="data">The data you want to send</param>
        /// <param name="ack">If endpoint responds a message, ack will be invoke with response data.</param>
        /// <exception cref="WebSocketException">When
        /// <see cref="WebSocket.SendAsync(ArraySegment{byte},WebSocketMessageType,bool,CancellationToken)">
        /// SendAsync()</see> got wrong.</exception>
        protected void Emit(string name, byte[] data, EventReturn ack = null)
        {
            EventMessage eventMessage;
            if (ack == null)
            {
                eventMessage = new EventMessage {Name = name, Data = data, CallBackId = null};
            }
            else
            {
                var callBackId = RandomString(_callBackIdRandomLength);
                eventMessage = new EventMessage {Name = name, Data = data, CallBackId = callBackId};
                lock (EventResponses)
                {
                    EventResponses.Add(callBackId, ack);
                }
            }

            SendAsync(eventMessage.ToMessagePack());
        }

        public virtual void Emit<TSent, TReturned>(string name, TSent data, EventReturn<TReturned> ack = null)
        {
            if (data is byte[] && (ack is EventReturn<byte[]> || ack is EventReturn))
            {
                Emit(name, data as byte[], ack as EventReturn);
            }
            else
            {
                throw new InvalidCastException("Cannot emit non byte[] data");
            }
        }

        /// <summary>
        /// Handle a event with byte array data.
        /// </summary>
        /// <param name="name">Event name</param>
        /// <param name="callBack">Will be invoke with data from endpoint</param>
        protected void On(string name, EventAction callBack)
        {
            if (name == null) throw new Exception("Event name cannot be null.");
            lock (EventHandlers)
            {
                if (!EventHandlers.ContainsKey(name))
                {
                    EventHandlers.Add(name, null);
                }

                EventHandlers[name] = EventHandlers[name] + callBack;
            }
        }

        public virtual void On<TReceived, TReturned>(string name, EventAction<TReceived, TReturned> callBack)
        {
            if (callBack is EventAction<byte[], byte[]> || callBack is EventAction)
            {
                On(name, callBack as EventAction);
            }
            else
            {
                throw new InvalidCastException("Cannot handle non byte[] data");
            }
        }

        public void RemoveHandler(string name, EventAction callBack)
        {
            lock (EventHandlers)
            {
                if (!EventHandlers.ContainsKey(name))
                {
                    EventHandlers.Add(name, null);
                }

                // ReSharper disable once DelegateSubtraction
                EventHandlers[name] = EventHandlers[name] - callBack;
            }
        }

        public void RemoveAllHandler(string name)
        {
            lock (EventHandlers)
            {
                EventHandlers.Remove(name);
            }
        }

        public List<string> EventNames()
        {
            lock (EventHandlers)
            {
                return new List<string>(EventHandlers.Keys);
            }
        }

        public void Disconnect(WebSocketCloseStatus closeStatus = WebSocketCloseStatus.NormalClosure,
            string statusDescription = "")
        {
            DisconnectAsync(closeStatus, statusDescription).Wait();
        }

        public async Task DisconnectAsync(
            WebSocketCloseStatus closeStatus = WebSocketCloseStatus.NormalClosure,
            string statusDescription = "")
        {
            OnDisconnecting?.Invoke(closeStatus, statusDescription);
            _reading = false;
            _cancellation.Cancel();
            try
            {
                await RawSocket.CloseAsync(closeStatus, statusDescription, CancellationToken.None);
            }
            catch (Exception)
            {
                // Exit normally
            }
            OnDisconnect?.Invoke(closeStatus, statusDescription);
        }

        public async Task StartListen()
        {
            if (_reading) return;
            _reading = true;

            var send = Task.Run(StartSend);

            WebSocketReceiveResult closeResult = null;
            try
            {
                while (!_cancellation.IsCancellationRequested)
                {
                    try
                    {
                        var (result, message) = await ReceiveFullMessage();
                        if (result.MessageType == WebSocketMessageType.Binary)
                        {
                            var array = message.ToArray();
                            var memStream = new MemoryStream(array, 0, array.Length);
                            HandleMessage(memStream);
                        }

                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            closeResult = result;
                            break;
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // Exit normally
                    }
                }
                _reading = false;
                await send;

                WebSocketCloseStatus closeStatus = WebSocketCloseStatus.NormalClosure;
                if (closeResult?.CloseStatus != null) closeStatus = (WebSocketCloseStatus) closeResult.CloseStatus;
                await DisconnectAsync(closeStatus, closeResult?.CloseStatusDescription);
            }
            catch (WebSocketException e) when (e.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
            {
                OnDisconnect?.Invoke(WebSocketCloseStatus.ProtocolError, "ConnectionClosedPrematurely");
            }
            catch (Exception e)
            {
                Console.WriteLine("RECV ERR");
                Console.WriteLine(e);
                /*Disconnect();
                RawSocket.Dispose();*/
            }
        }

        private async Task<(WebSocketReceiveResult, IEnumerable<byte>)> ReceiveFullMessage()
        {
            WebSocketReceiveResult response;
            var message = new List<byte>();

            var buffer = new byte[4096];
            do
            {
                response = await RawSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cancellation.Token);
                message.AddRange(new ArraySegment<byte>(buffer, 0, response.Count));
            } while (!response.EndOfMessage);

            return (response, message);
        }

        /// <summary>
        /// Send data when <see cref="SendQueue">_sendQueue</see> has data.
        /// </summary>
        /// <returns></returns>
        protected async Task StartSend()
        {
            try
            {
                while (!_cancellation.IsCancellationRequested)
                {
                    if (SendQueue.TryDequeue(out var message))
                    {
                        var sendBuffer = new ArraySegment<Byte>(message, 0, message.Length);
                        try
                        {
                            await RawSocket.SendAsync(sendBuffer, WebSocketMessageType.Binary, true,
                                _cancellation.Token);
                        }
                        catch (OperationCanceledException)
                        {
                            // Exit normally
                        }
                    }
                    else
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(20));
                    }
                }
            }
            catch (WebSocketException e) when (e.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
            {
                OnDisconnect?.Invoke(WebSocketCloseStatus.ProtocolError, "ConnectionClosedPrematurely");
            }
            catch (Exception e)
            {
                Console.WriteLine("SEND ERR");
                Console.WriteLine(e);
                /*Disconnect();
                RawSocket.Dispose();*/
            }
            finally
            {
                _cancellation.Cancel();
            }
        }

        private void SendAsync(byte[] b)
        {
            SendQueue.Enqueue(b);
        }

        private static readonly Random Random = new Random();

        private static string RandomString(int length)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[Random.Next(s.Length)]).ToArray());
        }
    }

    /// <summary>
    /// This socket will communicate with string data.
    /// </summary>
    public class StringSocket : Socket
    {
        public StringSocket(WebSocket rawSocket, INamespace from, int bufferSize = 1024 * 4)
            : base(rawSocket, from, bufferSize)
        {
        }

        public void On(string name, EventAction<string, string> callBack)
        {
            base.On(name,
                (data, respond) => { callBack.Invoke(Cast(data), value => { respond.Invoke(Cast(value)); }); });
        }

        public override void On<TReceived, TReturned>(string name, EventAction<TReceived, TReturned> callBack)
        {
            if (callBack is EventAction<string, string>)
            {
                On(name, callBack as EventAction<string, string>);
            }
            else
            {
                base.On(name, callBack as EventAction);
            }
        }

        public void Emit(string name, string data, EventReturn<string> ack = null)
        {
            base.Emit(name, Cast(data), value => { ack?.Invoke(Cast(value)); });
        }

        public override void Emit<TSent, TReturned>(string name, TSent data, EventReturn<TReturned> ack = null)
        {
            if (data is string && (ack == null || ack is EventReturn<string>))
            {
                Emit(name, data as string, ack as EventReturn<string>);
            }
            else
            {
                base.Emit(name, data, ack);
            }
        }

        private string Cast(byte[] bytes)
        {
            return Encoding.UTF8.GetString(bytes);
        }

        private byte[] Cast(string str)
        {
            return Encoding.UTF8.GetBytes(str);
        }
    }
}