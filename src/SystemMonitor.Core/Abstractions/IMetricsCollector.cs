using SystemMonitor.Core.Models;

namespace SystemMonitor.Core.Abstractions;

/// <summary>
/// Defines a strategy for reading the current system resource usage.
/// </summary>
/// <remarks>
/// Each operating system exposes CPU, memory and disk information differently
/// (for example, Windows uses <c>PerformanceCounter</c>). Hiding that behind this
/// interface lets the core monitoring loop stay platform-agnostic: a new platform
/// can be supported by adding another implementation without changing any other code.
/// </remarks>
public interface IMetricsCollector
{
    /// <summary>
    /// Reads a single snapshot of the current CPU, memory and disk usage.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A <see cref="SystemMetrics"/> snapshot for the current moment.</returns>
    Task<SystemMetrics> CollectAsync(CancellationToken cancellationToken = default);
}
