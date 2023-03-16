[![License](https://img.shields.io/github/license/snowberry-software/Snowberry.IPC)](https://github.com/snowberry-software/Snowberry.IPC/blob/master/LICENSE)
[![NuGet Version](https://img.shields.io/nuget/v/Snowberry.IPC.svg?logo=nuget)](https://www.nuget.org/packages/Snowberry.IPC/)

# Snowberry.IPC

Interprocess communication (IPC) library utilizes named pipes to allow communication between different processes running on the same machine.

# How to use it

## Server

```cs
using var serverPipe = new ServerPipe("EXAMPLE_PIPE", "Example Server", CancellationToken.None)
{
    UseDynamicDataPacketSize = true
};

serverPipe.DataReceived += (s, e) =>
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("Client [Len = {0}]: {1}", e.Length, Encoding.UTF8.GetString(e.Data));
    Console.ResetColor();
};

serverPipe.GotConnectionEvent += (sender, e) =>
{
    Console.WriteLine("Server pipe got a new client...");
};

serverPipe.PipeClosed += (_, _) =>
{
    Console.WriteLine("Server pipe got closed...");
};

await serverPipe.WaitForClientAsync();

...

await serverPipe.WriteAsync(Encoding.UTF8.GetBytes("This is sent from the server!"));

```

## Client

```cs
using var clientPipe = new ClientPipe(".", "EXAMPLE_PIPE", "Example Client")
{
    UseDynamicDataPacketSize = true
};

clientPipe.DataReceived += (s, e) =>
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("Server [Len = {0}]: {1}", e.Length, Encoding.UTF8.GetString(e.Data));
    Console.ResetColor();
};

clientPipe.PipeClosed += (_, _) =>
{
    Console.WriteLine("Server pipe got closed...");
};

await clientPipe.ConnectAsync();

clientPipe.StartReadingAsync(CancellationToken.None);

...

await clientPipe.WriteAsync(Encoding.UTF8.GetBytes("This is sent from the client!"));
```
