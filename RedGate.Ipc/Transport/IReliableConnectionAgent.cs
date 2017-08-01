using System;

namespace RedGate.Ipc
{
    /// <summary>
    /// Provides reconnection logic for <see cref="IConnection"/> consumers.
    /// </summary>
    /// <remarks>
    /// An IReliableConnectionAgent will attempt to keep an active connection
    /// cached at all times until the agent is disposed. So calling IConnection.Dispose()
    /// on a retuned connection will cause the agent to generate another on a background
    /// thread in advance of the next TryGetConnection call. To properly terminate connection,
    /// dispose the IReliableConnectionAgent.
    /// </remarks>
    public interface IReliableConnectionAgent : IDisposable
    {
        /// <summary>
        /// Returns an <see cref="IConnection"/>, or null if a live connection could not supplied within the specified time frame.
        /// Mutliple calls to TryGetConnection will yield the same <see cref="IConnection"/> if it remains connected and undisposed.
        /// </summary>
        /// <param name="timeoutMs">
        /// The maximum time in milliseconds to wait before giving up and returning null.
        /// </param>
        /// <exception cref="ObjectDisposedException">If IReliableConnectionAgent was disposed before or during the method call.</exception>
        IConnection TryGetConnection(int timeoutMs);
    }
}