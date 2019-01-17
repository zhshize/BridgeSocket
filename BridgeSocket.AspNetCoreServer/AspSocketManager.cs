using System;
using System.Net.WebSockets;
using System.Threading.Tasks;
using BridgeSocket.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace BridgeSocket.AspNetCoreServer
{
    /// <summary>
    /// AspSocketManager is for asp.net core application.
    /// </summary>
    public class AspSocketManager : SocketManager<StringSocket>
    {
        public WebSocketOptions Options;

        public AspSocketManager(WebSocketOptions options = null)
        {
            Options = options;
        }

        public void Attach(IApplicationBuilder app)
        {
            app.Use(GetWebSocketMiddleware());
        }
        
        protected StringSocket ConstructSocket(WebSocket rawSocket, INamespace from)
        {
            return new StringSocket(rawSocket, from, Options.ReceiveBufferSize);
        }

        private Func<HttpContext, Func<Task>, Task> GetWebSocketMiddleware()
        {
            return async (context, next) =>
            {
                if (context.WebSockets.IsWebSocketRequest)
                {
                    using (var webSocket = await context.WebSockets.AcceptWebSocketAsync())
                    {
                        INamespace nsp = Of(context.Request.Path);
                        var socket = ConstructSocket(webSocket, nsp);
                        nsp.AddSocket(socket);
                        await socket.StartListen();
                    }
                }
                else
                {
                    await next();
                }
            };
        }
    }
}