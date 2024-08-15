using Microsoft.Extensions.Logging;
using System;

namespace RapidBus;

/// <summary>
/// Represents a persistent connection..
/// </summary>
public interface IPersistentConnection
{
    /// <summary>
    /// Event that is raised when the connection is reestablished after a connection failure.
    /// </summary>
    event EventHandler<bool> ReconnectedAfterConnectionFailure;

    /// <summary>
    /// Gets a value indicating whether this instance is connected.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Tries to establish a connection.
    /// </summary>
    /// <returns>True if the connection is successfully established, false otherwise.</returns>
    bool TryConnect();
}
