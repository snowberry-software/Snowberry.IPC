namespace Snowberry.IPC;

/// <summary>
/// The reason why the pipe got closed.
/// </summary>
public enum PipeCloseReason : byte
{
    /// <summary>
    /// Gets called when the connection between the client and server closes.
    /// </summary>
    NoConnection,

    /// <summary>
    /// Gets called when disposing the instance.
    /// </summary>
    Dispose
}
