using System.Text;
using Snowberry.IPC;

Console.Title = "Example Server";

using var serverPipe = new ServerPipe("EXAMPLE_PIPE", "Example Server", CancellationToken.None)
{
    UseDynamicDataPacketSize = true
};

serverPipe.Initialize();

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

Console.WriteLine("Waiting for client to connect...");
await serverPipe.WaitForClientAsync();

Console.WriteLine("IsConnected: {0}", serverPipe.IsConnected);
while (serverPipe.IsConnected)
{
    string data = Console.ReadLine() ?? "";

    if (!serverPipe.IsConnected)
        break;

    if (string.IsNullOrWhiteSpace(data))
        continue;

    await serverPipe.WriteAsync(Encoding.UTF8.GetBytes(data));
}