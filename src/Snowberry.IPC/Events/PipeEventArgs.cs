using System;

namespace Snowberry.IPC.Events;

/// <summary>
/// The pipe data event.
/// </summary>
public class PipeEventArgs : EventArgs
{
    public PipeEventArgs(byte[] data, int dataLength, bool usedDynamicallyRead)
    {
        Data = data;
        Length = dataLength;
        UsedDynamicallyRead = usedDynamicallyRead;
    }

    /// <summary>
    /// The data.
    /// </summary>
    public byte[] Data { get; }

    /// <summary>
    /// The length of the data.
    /// </summary>
    public int Length { get; }

    /// <summary>
    /// Whether the data has been read 'dynamically'.
    /// </summary>
    public bool UsedDynamicallyRead { get; }
}