# BridgeSocket

BridgeSocket is a real-time library inspired by [socket.io](https://socket.io/).

# Why do I reinvent the wheel?

A simple reason: I need a pure WebSocket-base library can easily implemented in any
 programming language/framework/platform.

BridgeSocket is a part from my personal commercial project, and C# version is just 
only one library in BridgeSocket family.  Other version (i.g. Browser Javascript, 
node.js, ...) is under development.

# Requirement

 - .Net Core 2.2
 
Requirement for `BridgeSocket.AspNetCoreServer`:

 - Asp.net Core 2.2
 
# Installation

Server with Asp.net Core
```
dotnet add package BridgeSocket.AspNetCoreServer
```
Client
```
dotnet add package BridgeSocket.Client
```

# Usage

## With Asp.net Core Server

Server-side support is from `BridgeSocket.AspNetCoreServer`.

### in `Startup.cs`

```C#
using BridgeSocket.AspNetCoreServer;
using BridgeSocket.Core;

public class Startup
{
    // ...
    public void Configure(IApplicationBuilder app, IHostingEnvironment env)
    {
        // other configuration code ...
        
        var WsOptions = new WebSocketOptions()
        {
            KeepAliveInterval = TimeSpan.FromMinutes(2),
            ReceiveBufferSize = 1024 * 4
        };
                
        var manager = new AspSocketManager();
        manager.Options = WsOptions;
        manager.OnConnection += socket =>
        {
            socket.On("hello", (data, respond) =>
            {
                Console.WriteLine("hello from " + data);
                respond("hi");
            });
        };
    }
}
```
Client-side support is from `BridgeSocket.Client`.

### Client-side code

```C#
using BridgeSocket.Client;
using BridgeSocket.Core;

class Program
{
    static void Main(string[] args)
    {
        StringSocket socket = BridgeSocketClient.Connect(new Uri("wss://localhost:5001/"), CancellationToken.None);
        socket.On<string, string>("hello", (data, respond) => Console.WriteLine(data));
        
        while (true)
        {
            var str = Console.ReadLine();
            if (str == "exit") break;
            socket.Emit<string, string>("hello", str, value => Console.WriteLine("ack:" + value));
        }
    }
}
```

# License

Copyright 2019 zhshize

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.