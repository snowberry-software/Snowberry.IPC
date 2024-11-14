using System.Text;
using Snowberry.IPC.NamedPipes;

Console.Title = "Example Client";

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

clientPipe.PipeClosed += (_, reason) =>
{
    Console.WriteLine("Client pipe got closed ({0})...", reason);
};

Console.WriteLine("Waiting for server connection...");
await clientPipe.ConnectAsync();
Console.WriteLine("Client is connected!");

clientPipe.StartReadingAsync(CancellationToken.None);

while (clientPipe.IsConnected)
{
    string data = Console.ReadLine() ?? "";

    if (!clientPipe.IsConnected)
        break;

    if (string.IsNullOrWhiteSpace(data))
        continue;

    if (data[0] == 'x')
    {
        await clientPipe.WriteAsync(Encoding.UTF8.GetBytes(new string('A', BasePipe.MaxBufferLength * 2)));
        continue;
    }

    await clientPipe.WriteAsync(Encoding.UTF8.GetBytes(data));
}