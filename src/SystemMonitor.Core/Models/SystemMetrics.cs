namespace SystemMonitor.Core.Models;

/// <summary>
/// An immutable snapshot of system resource usage taken at a single point in time.
/// </summary>
/// <remarks>
/// This is a <c>record</c>, so once created its values never change. Each monitoring
/// cycle produces a fresh <see cref="SystemMetrics"/> instance that is passed to every plugin.
/// Memory and disk values are expressed in megabytes (MB) to match the required API payload.
/// </remarks>
/// <param name="CpuPercent">Total system CPU utilisation as a percentage (0-100).</param>
/// <param name="RamUsedMb">Physical RAM currently in use, in megabytes.</param>
/// <param name="RamTotalMb">Total physical RAM installed, in megabytes.</param>
/// <param name="DiskUsedMb">Disk space currently used on the monitored drive, in megabytes.</param>
/// <param name="DiskTotalMb">Total size of the monitored drive, in megabytes.</param>
/// <param name="TimestampUtc">The UTC time at which this snapshot was captured.</param>
public sealed record SystemMetrics(
    double CpuPercent,
    double RamUsedMb,
    double RamTotalMb,
    double DiskUsedMb,
    double DiskTotalMb,
    DateTimeOffset TimestampUtc);
