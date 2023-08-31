using System;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace Snowberry.IPC;

/// <summary>
/// The server pipe type.
/// </summary>
public class ServerPipe : BasePipe
{
    private readonly CancellationToken _cancellationToken;

    public event EventHandler? GotConnectionEvent;

    public ServerPipe(string pipeName, string? pipeDebugName, CancellationToken cancellationToken) : base(pipeDebugName)
    {
        _pipeStream = new NamedPipeServerStream(pipeName,
                PipeDirection.InOut,
                NamedPipeServerStream.MaxAllowedServerInstances,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous);

        OnPipeStreamInitialized();

        _cancellationToken = cancellationToken;
    }

    /// <summary>
    /// Initializes the server pipe and asynchronously waits for the client before starting to read data.
    /// </summary>
    public virtual void Initialize()
    {
        (_pipeStream as NamedPipeServerStream)?.BeginWaitForConnection(new AsyncCallback(GotPipeConnectionStatic), this);
    }

    /// <inheritdoc/>
    protected override void OnPipeStreamInitialized()
    {
    }

    protected static void GotPipeConnectionStatic(IAsyncResult result)
    {
        var pipeServer = result.AsyncState as ServerPipe;
        pipeServer?.GotPipeConnection(result);
    }

    /// <summary>
    /// Waits for a client to connect.
    /// </summary>
    public virtual async Task WaitForClientAsync()
    {
        _ = _pipeStream ?? throw new NullReferenceException(nameof(_pipeStream));

        if (_pipeStream.IsConnected)
        {
            _protectedIsConnected = true;
            return;
        }

        await ((NamedPipeServerStream)_pipeStream).WaitForConnectionAsync();
        _protectedIsConnected = true;
    }

    protected virtual void GotPipeConnection(IAsyncResult result)
    {
        _ = _pipeStream ?? throw new NullReferenceException(nameof(_pipeStream));

        ((NamedPipeServerStream)_pipeStream).EndWaitForConnection(result);

        GotConnectionEvent?.Invoke(this, new EventArgs());

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        StartReadingAsync(_cancellationToken);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
    }
}
