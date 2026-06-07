using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SystemMonitor.Core.Abstractions;
using SystemMonitor.Core.Models;
using SystemMonitor.Infrastructure.Configuration;

namespace SystemMonitor.Infrastructure.Collectors;

/// <summary>
/// A cross-platform collector used when no platform-specific implementation is available
/// (for example on Linux or macOS).
/// </summary>
/// <remarks>
/// Disk usage works on every platform, so it is reported accurately. CPU and memory are
/// reported as 0 with a warning, because reading them requires platform-specific code that
/// is intentionally out of scope. This class demonstrates the extension point: a full
/// <c>LinuxMetricsCollector</c> or <c>MacMetricsCollector</c> can be added later without
/// changing any other part of the application.
/// </remarks>
public sealed class FallbackMetricsCollector : IMetricsCollector
{
    private readonly MonitorOptions _options;
    private readonly ILogger<FallbackMetricsCollector> _logger;
    private bool _warned;

    public FallbackMetricsCollector(IOptions<MonitorOptions> options, ILogger<FallbackMetricsCollector> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<SystemMetrics> CollectAsync(CancellationToken cancellationToken = default)
    {
        if (!_warned)
        {
            _logger.LogWarning(
                "Using the fallback collector: CPU and memory are not implemented on this platform and will report 0. Disk usage is fully supported.");
            _warned = true;
        }

        var (diskUsedMb, diskTotalMb) = DiskUsageReader.Read(_options);

        var metrics = new SystemMetrics(
            CpuPercent: 0d,
            RamUsedMb: 0d,
            RamTotalMb: 0d,
            DiskUsedMb: diskUsedMb,
            DiskTotalMb: diskTotalMb,
            TimestampUtc: DateTimeOffset.UtcNow);

        return Task.FromResult(metrics);
    }
}
