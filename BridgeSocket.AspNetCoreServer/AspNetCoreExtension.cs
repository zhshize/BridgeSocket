using Microsoft.AspNetCore.Builder;

namespace BridgeSocket.AspNetCoreServer
{
    public static class AspNetCoreExtension
    {
        /// <summary>
        /// Adds BridgeSocket to the IApplicationBuilder request execution pipeline.
        /// </summary>
        /// <param name="app">The IApplicationBuilder</param>
        /// <param name="manager">The SocketManager</param>
        public static void UseBridgeSocket(this IApplicationBuilder app, AspSocketManager manager)
        {
            manager.Attach(app);
        }
    }
}