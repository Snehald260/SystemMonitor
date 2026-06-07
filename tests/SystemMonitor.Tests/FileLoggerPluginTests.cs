using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SystemMonitor.Core.Models;
using SystemMonitor.Plugins;
using SystemMonitor.Plugins.Configuration;

namespace SystemMonitor.Tests;

public class FileLoggerPluginTests
{
    [Fact]
    public async Task Writes_a_line_containing_the_metric_values()
    {
        var path = Path.Combine(Path.GetTempPath(), $"sysmon-test-{Guid.NewGuid():N}.log");
        try
        {
            var options = Options.Create(new FileLoggerOptions { Enabled = true, FilePath = path });
            var plugin = new FileLoggerPlugin(options, NullLogger<FileLoggerPlugin>.Instance);

            var metrics = new SystemMetrics(
                CpuPercent: 42.0,
                RamUsedMb: 1024,
                RamTotalMb: 8192,
                DiskUsedMb: 100,
                DiskTotalMb: 200,
                TimestampUtc: DateTimeOffset.UnixEpoch);

            await plugin.OnMetricsCollectedAsync(metrics);

            var content = await File.ReadAllTextAsync(path);
            Assert.Contains("cpu=42.0", content);
            Assert.Contains("ram_used=1024", content);
            Assert.Contains("disk_used=100", content);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public async Task Appends_one_line_per_call()
    {
        var path = Path.Combine(Path.GetTempPath(), $"sysmon-test-{Guid.NewGuid():N}.log");
        try
        {
            var options = Options.Create(new FileLoggerOptions { Enabled = true, FilePath = path });
            var plugin = new FileLoggerPlugin(options, NullLogger<FileLoggerPlugin>.Instance);

            var metrics = new SystemMetrics(1, 2, 3, 4, 5, DateTimeOffset.UnixEpoch);

            await plugin.OnMetricsCollectedAsync(metrics);
            await plugin.OnMetricsCollectedAsync(metrics);
            await plugin.OnMetricsCollectedAsync(metrics);

            var lines = await File.ReadAllLinesAsync(path);
            Assert.Equal(3, lines.Length);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
