using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Snowberry.IPC.NamedPipes.Events;

namespace Snowberry.IPC.NamedPipes;

/// <summary>
/// The base type for the pipe server and client.
/// </summary>
public abstract class BasePipe : IDisposable
{
    public const int MaxBufferLength = 1024 * 1024;

    /// <summary>
    /// Gets fired when new data is received.
    /// </summary>
    public event EventHandler<PipeEventArgs>? DataReceived;

    /// <summary>
    /// Gets called when the pipe gets closed by losing the connection between the client and the server.
    /// </summary>
    public event EventHandler<PipeCloseReason>? PipeClosed;

    protected readonly string? _pipeDebugName;
    protected PipeStream? _pipeStream;
    protected bool _protectedIsConnected;

    protected bool _useDynamicDataPacketSize;

    /// <summary>
    /// Creates a new base pipe.
    /// </summary>
    /// <param name="pipeDebugName">The optional debug name.</param>
    /// <param name="pipeStream">The stream of the pipe.</param>
    /// <param name="useDynamicDataPacketSize">Whether to use <see cref="UseDynamicDataPacketSize"/>.</param>
    public BasePipe(string? pipeDebugName, PipeStream pipeStream, bool useDynamicDataPacketSize = false)
    {
        _pipeDebugName = pipeDebugName;
        _pipeStream = pipeStream ?? throw new ArgumentNullException(nameof(pipeStream));
        _useDynamicDataPacketSize = useDynamicDataPacketSize;
    }

    /// <summary>
    /// Creates a new base pipe.
    /// </summary>
    /// <param name="pipeDebugName">The optional debug name</param>
    protected BasePipe(string? pipeDebugName)
    {
        _pipeDebugName = pipeDebugName;
    }

    /// <summary>
    /// Gets called when the pipe stream has been initialized.
    /// </summary>
    protected abstract void OnPipeStreamCreated();

    /// <summary>
    /// Gets called when the pipe closes.
    /// </summary>
    /// <param name="reason">The reason for the close.</param>
    /// <remarks>
    /// Could be called multiple types (see <paramref name="reason"/>).<para/>
    /// Gets called before <see cref="PipeClosed"/>.
    /// </remarks>
    public virtual void OnPipeClosed(PipeCloseReason reason)
    {
    }

    /// <summary>
    /// Gets called when receiving new data.
    /// </summary>
    /// <param name="buffer">The buffer.</param>
    /// <param name="length">The length of the received data.</param>
    /// <remarks>
    /// Gets called before <see cref="DataReceived"/>.
    /// </remarks>
    public virtual void OnDataReceived(byte[] buffer, int length)
    {
    }

    /// <summary>
    /// Starts reading asynchronously from the pipe stream...
    /// </summary>
    /// <param name="token">The cancellation token.</param>
    public async Task StartReadingAsync(CancellationToken token)
    {
        _ = _pipeStream ?? throw new NullReferenceException(nameof(_pipeStream));

        if (MaxBufferLength < 4)
            throw new ArgumentOutOfRangeException(nameof(MaxBufferLength), $"The {nameof(MaxBufferLength)} must be at least 4 bytes.");

        byte[] buffer = new byte[MaxBufferLength];

        if (!UseDynamicDataPacketSize)
        {
            await HandleStaticBufferAsync(buffer, token);
            return;
        }

        await HandleDynamicBufferAsync(buffer, token);
    }

    protected virtual async Task HandleStaticBufferAsync(byte[] buffer, CancellationToken token)
    {
#if NET6_0_OR_GREATER
        int readLength = await _pipeStream!.ReadAsync(buffer.AsMemory(0, MaxBufferLength), token);
#else
        int readLength = await _pipeStream!.ReadAsync(buffer, 0, MaxBufferLength, token);
#endif

        if (IsPipeClosed(readLength, token))
            return;

        byte[] readBytes = new byte[readLength];
        Array.Copy(buffer, readBytes, readLength);

        OnDataReceived(readBytes, readLength);
        DataReceived?.Invoke(this, new PipeEventArgs(readBytes, readLength, usedDynamicallyRead: false));
        await StartReadingAsync(token);
    }

    protected virtual async Task HandleDynamicBufferAsync(byte[] buffer, CancellationToken token)
    {
        int expectedRead = -1;
        var dynamicBuffer = new List<byte>();
        do
        {
#if NET6_0_OR_GREATER
            int readLength = await _pipeStream!.ReadAsync(buffer.AsMemory(0, MaxBufferLength), token);
#else
            int readLength = await _pipeStream!.ReadAsync(buffer, 0, MaxBufferLength, token);
#endif

            if (IsPipeClosed(readLength, token))
                return;

            if (expectedRead == -1)
            {
                expectedRead = BitConverter.ToInt32(buffer, 0);

                if (readLength - 4 == expectedRead)
                {
#if NET6_0_OR_GREATER
                    byte[] actualBuffer = buffer.AsSpan()[4..readLength].ToArray();
#else
                    byte[] actualBuffer = buffer.Skip(4).Take(expectedRead).ToArray();
#endif

                    OnDataReceived(actualBuffer, actualBuffer.Length);
                    DataReceived?.Invoke(this, new PipeEventArgs(actualBuffer, actualBuffer.Length, usedDynamicallyRead: true));
                    await StartReadingAsync(token);
                    return;
                }
            }

            dynamicBuffer.AddRange(buffer.Take(readLength));
        } while (IsConnected && dynamicBuffer.Count < expectedRead);

        buffer = [.. dynamicBuffer];
        OnDataReceived(buffer, buffer.Length);
        DataReceived?.Invoke(this, new PipeEventArgs(buffer, buffer.Length, usedDynamicallyRead: true));

        await StartReadingAsync(token);
    }

    protected virtual bool IsPipeClosed(int readLength, CancellationToken token)
    {
        if (!token.IsCancellationRequested && readLength != 0)
            return false;

        _protectedIsConnected = false;
        OnPipeClosed(PipeCloseReason.NoConnection);
        PipeClosed?.Invoke(this, PipeCloseReason.NoConnection);
        return true;
    }

    /// <summary>
    /// Writes data asynchronously.
    /// </summary>
    /// <param name="data">The data to write.</param>
    /// <param name="offset">The data offset.</param>
    /// <param name="length">The data length.</param>
    public virtual Task WriteAsync(byte[] data, int offset, int length)
    {
        _ = _pipeStream ?? throw new NullReferenceException(nameof(_pipeStream));

        if (UseDynamicDataPacketSize)
        {
#if NET6_0_OR_GREATER
            var packet = new List<byte>(data.AsSpan().Slice(offset, length).ToArray());
#else
            var packet = new List<byte>(data.Skip(offset).Take(count: length));
#endif
            packet.InsertRange(0, BitConverter.GetBytes(length));

            return _pipeStream.WriteAsync([.. packet], 0, packet.Count);
        }

        return _pipeStream.WriteAsync(data, offset, length);
    }

    /// <summary>
    /// Writes data asynchronously.
    /// </summary>
    /// <param name="data">The data to write.</param>
    public virtual Task WriteAsync(byte[] data)
    {
        return WriteAsync(data, 0, data.Length);
    }

    /// <inheritdoc/>
    public override string? ToString()
    {
        return PipeDebugName ?? base.ToString();
    }

    /// <inheritdoc/>
    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);

        if (_pipeStream == null)
            return;

        if (_pipeStream.IsConnected)
#if NET6_0_OR_GREATER
            if (OperatingSystem.IsWindows())
#endif
                _pipeStream.WaitForPipeDrain();

        _pipeStream.Close();
        OnPipeClosed(PipeCloseReason.Dispose);
        PipeClosed?.Invoke(this, PipeCloseReason.Dispose);
        _pipeStream.Dispose();
        _pipeStream = null!;
    }

    /// <summary>
    /// The debug name of the pipe.
    /// </summary>
    public string? PipeDebugName => _pipeDebugName;

    /// <summary>
    /// Determines whether the pipe stream is connected.
    /// </summary>
    public bool IsConnected =>
            // NOTE(VNC):
            //
            // Only using the `IsConnected` property is unreliable.
            // The property could be true even though the pipe is already broken.
            //
            _pipeStream != null && _pipeStream.IsConnected && _protectedIsConnected;

    /// <summary>
    /// Determines whether to use a dynamic buffer size.
    /// </summary>
    /// <remarks>
    /// The length of the data will be written as 32bit-integer before the payload if this is enabled.<para/>
    /// The read operation will only be completed if the data length is exactly as the length that has been provided.<para/>
    /// If this is disabled and the incoming payload is longer than <see cref="MaxBufferLength"/> then it will be split to two different read cycles.<para/>
    /// </remarks>
    public bool UseDynamicDataPacketSize
    {
        get => _useDynamicDataPacketSize;
        set => _useDynamicDataPacketSize = value;
    }

    /// <summary>
    /// The pipe stream that is used.
    /// </summary>
    public PipeStream? Stream => _pipeStream;
}
