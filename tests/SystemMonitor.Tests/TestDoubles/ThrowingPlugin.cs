using SystemMonitor.Core.Abstractions;
using SystemMonitor.Core.Models;

namespace SystemMonitor.Tests.TestDoubles;

/// <summary>
/// A plugin that always throws, used to verify that one failing plugin does not stop the
/// monitoring loop or prevent other plugins from being notified.
/// </summary>
internal sealed class ThrowingPlugin : IMonitorPlugin
{
    public string Name => "Throwing";

    public Task OnMetricsCollectedAsync(SystemMetrics metrics, CancellationToken cancellationToken = default)
        => throw new InvalidOperationException("Simulated plugin failure.");
}
