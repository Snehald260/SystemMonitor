using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SystemMonitor.Core.Abstractions;
using SystemMonitor.Core.Models;
using SystemMonitor.Infrastructure.Configuration;

namespace SystemMonitor.Infrastructure.Collectors;

/// <summary>
/// Collects CPU, memory and disk usage on Windows.
/// </summary>
/// <remarks>
/// CPU is read with a <see cref="PerformanceCounter"/>, physical memory with the
/// Win32 <c>GlobalMemoryStatusEx</c> API, and disk with the cross-platform
/// <see cref="DiskUsageReader"/>. The class is marked Windows-only so the rest of the
/// application can remain platform-agnostic behind <see cref="IMetricsCollector"/>.
/// </remarks>
[SupportedOSPlatform("windows")]
public sealed class WindowsMetricsCollector : IMetricsCollector, IDisposable
{
    private const double BytesPerMb = 1024d * 1024d;

    private readonly MonitorOptions _options;
    private readonly ILogger<WindowsMetricsCollector> _logger;
    private readonly PerformanceCounter _cpuCounter;
    private bool _disposed;

    public WindowsMetricsCollector(IOptions<MonitorOptions> options, ILogger<WindowsMetricsCollector> logger)
    {
        _options = options.Value;
        _logger = logger;

        // "% Processor Time" on the "_Total" instance gives overall CPU utilisation (0-100).
        _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total", readOnly: true);

        // The first reading from a CPU performance counter is always 0 because it needs a
        // previous sample to compare against. Take a throwaway "warm-up" reading here so the
        // first real cycle returns a meaningful value.
        _cpuCounter.NextValue();
    }

    /// <inheritdoc />
    public async Task<SystemMetrics> CollectAsync(CancellationToken cancellationToken = default)
    {
        // A performance counter needs a short gap between samples to compute a rate.
        await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);

        var cpuPercent = ReadCpuPercent();
        var (ramUsedMb, ramTotalMb) = ReadMemory();
        var (diskUsedMb, diskTotalMb) = DiskUsageReader.Read(_options);

        return new SystemMetrics(
            CpuPercent: cpuPercent,
            RamUsedMb: ramUsedMb,
            RamTotalMb: ramTotalMb,
            DiskUsedMb: diskUsedMb,
            DiskTotalMb: diskTotalMb,
            TimestampUtc: DateTimeOffset.UtcNow);
    }

    private double ReadCpuPercent()
    {
        try
        {
            return Math.Round(_cpuCounter.NextValue(), 2);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read CPU counter; reporting 0.");
            return 0d;
        }
    }

    private (double UsedMb, double TotalMb) ReadMemory()
    {
        var status = new MemoryStatusEx();
        if (!GlobalMemoryStatusEx(status))
        {
            _logger.LogWarning("GlobalMemoryStatusEx failed; reporting 0 memory usage.");
            return (0d, 0d);
        }

        var totalMb = status.ullTotalPhys / BytesPerMb;
        var usedMb = (status.ullTotalPhys - status.ullAvailPhys) / BytesPerMb;
        return (Math.Round(usedMb, 2), Math.Round(totalMb, 2));
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _cpuCounter.Dispose();
        _disposed = true;
    }

    // --- Win32 interop for physical memory ---

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private sealed class MemoryStatusEx
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;

        public MemoryStatusEx()
        {
            dwLength = (uint)Marshal.SizeOf(typeof(MemoryStatusEx));
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx([In, Out] MemoryStatusEx lpBuffer);
}
