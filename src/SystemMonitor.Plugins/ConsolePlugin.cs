using System.Globalization;
using SystemMonitor.Core.Abstractions;
using SystemMonitor.Core.Models;

namespace SystemMonitor.Plugins;

/// <summary>
/// A plugin that writes each metrics snapshot to the console in a human-readable format.
/// </summary>
public sealed class ConsolePlugin : IMonitorPlugin
{
    /// <inheritdoc />
    public string Name => "Console";

    /// <inheritdoc />
    public Task OnMetricsCollectedAsync(SystemMetrics metrics, CancellationToken cancellationToken = default)
    {
        var line = string.Format(
            CultureInfo.InvariantCulture,
            "[{0:HH:mm:ss}] CPU: {1,5:F1}% | RAM: {2,9:F0} / {3,9:F0} MB | Disk: {4,11:F0} / {5,11:F0} MB",
            metrics.TimestampUtc.ToLocalTime(),
            metrics.CpuPercent,
            metrics.RamUsedMb,
            metrics.RamTotalMb,
            metrics.DiskUsedMb,
            metrics.DiskTotalMb);

        Console.WriteLine(line);
        return Task.CompletedTask;
    }
}
