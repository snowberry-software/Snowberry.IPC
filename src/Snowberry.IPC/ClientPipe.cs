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
        OnPipeStreamInitialized();
    }

    /// <inheritdoc/>
    protected override void OnPipeStreamInitialized()
    {
    }

    /// <summary>
    /// Connects the client to the server pipe.
    /// </summary>
    /// <param name="timeout"> The optional number of milliseconds to wait for the server to respond before the connection times out.</param>
    public async Task<bool> ConnectAsync(int? timeout = null)
    {
        _ = _pipeStream ?? throw new NullReferenceException(nameof(_pipeStream));
        _protectedIsConnected = false;

        if (timeout == null)
            await ((NamedPipeClientStream)_pipeStream).ConnectAsync();
        else
        {
            try
            {
                await ((NamedPipeClientStream)_pipeStream).ConnectAsync(timeout.Value);
            }
            catch (TimeoutException)
            {
                return false;
            }
        }

        if (_pipeStream.IsConnected)
        {
            _protectedIsConnected = true;
            return true;
        }

        return false;
    }
}