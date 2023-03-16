using System;
using System.IO.Pipes;
using System.Threading.Tasks;

namespace Snowberry.IPC;

/// <summary>
/// The client pipe type.
/// </summary>
public class ClientPipe : BasePipe
{
    public ClientPipe(string serverName, string pipeName, string? pipeDebugName) : base(pipeDebugName)
    {
        _pipeStream = new NamedPipeClientStream(serverName, pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
    }

    /// <summary>
    /// Connects the client to the server pipe.
    /// </summary>
    public async Task ConnectAsync()
    {
        _ = _pipeStream ?? throw new NullReferenceException(nameof(_pipeStream));
        await ((NamedPipeClientStream)_pipeStream).ConnectAsync();
        _protectedIsConnected = true;
    }
}
