using System;

namespace Snowberry.IPC.Events;

/// <summary>
/// The pipe data event.
/// </summary>
public class PipeEventArgs : EventArgs
{
    public PipeEventArgs(byte[] data, int dataLength)
    {
        Data = data;
        Length = dataLength;
    }

    /// <summary>
    /// The data.
    /// </summary>
    public byte[] Data { get; }

    /// <summary>
    /// The length of the data.
    /// </summary>
    public int Length { get; }
}