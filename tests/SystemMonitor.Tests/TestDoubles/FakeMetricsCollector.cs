using SystemMonitor.Core.Abstractions;
using SystemMonitor.Core.Models;

namespace SystemMonitor.Tests.TestDoubles;

/// <summary>
/// A metrics collector that returns a fixed snapshot, so tests do not depend on real hardware.
/// </summary>
internal sealed class FakeMetricsCollector : IMetricsCollector
{
    private readonly SystemMetrics _metrics;

    public FakeMetricsCollector(SystemMetrics? metrics = null)
    {
        _metrics = metrics ?? new SystemMetrics(
            CpuPercent: 12.5,
            RamUsedMb: 4096,
            RamTotalMb: 16384,
            DiskUsedMb: 250000,
            DiskTotalMb: 500000,
            TimestampUtc: DateTimeOffset.UnixEpoch);
    }

    public int CollectCount { get; private set; }

    public Task<SystemMetrics> CollectAsync(CancellationToken cancellationToken = default)
    {
        CollectCount++;
        return Task.FromResult(_metrics);
    }
}
