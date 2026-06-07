using SystemMonitor.Core.Models;

namespace SystemMonitor.Core.Abstractions;

/// <summary>
/// Contract that every monitoring plugin must implement.
/// </summary>
/// <remarks>
/// The monitoring loop notifies all registered plugins on each cycle by calling
/// <see cref="OnMetricsCollectedAsync"/>. Plugins decide what to do with the data
/// (print it, write it to a file, post it to an API, etc.). New behaviour can be
/// added simply by implementing this interface and registering it &mdash; the core
/// logic never needs to change.
/// </remarks>
public interface IMonitorPlugin
{
    /// <summary>
    /// A short, human-readable name used in logs and diagnostics.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Called once per monitoring cycle with the latest metrics snapshot.
    /// </summary>
    /// <param name="metrics">The metrics captured for the current cycle.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task OnMetricsCollectedAsync(SystemMetrics metrics, CancellationToken cancellationToken = default);
}
