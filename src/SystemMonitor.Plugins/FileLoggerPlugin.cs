using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SystemMonitor.Core.Abstractions;
using SystemMonitor.Core.Models;
using SystemMonitor.Plugins.Configuration;

namespace SystemMonitor.Plugins;

/// <summary>
/// A plugin that appends each metrics snapshot as a line of text to a configurable file.
/// This is the required "log to a local file" sample plugin.
/// </summary>
public sealed class FileLoggerPlugin : IMonitorPlugin
{
    private readonly FileLoggerOptions _options;
    private readonly ILogger<FileLoggerPlugin> _logger;

    // Serialises writes so concurrent cycles can never interleave or corrupt the file.
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public FileLoggerPlugin(IOptions<FileLoggerOptions> options, ILogger<FileLoggerPlugin> logger)
    {
        _options = options.Value;
        _logger = logger;

        EnsureDirectoryExists();
    }

    /// <inheritdoc />
    public string Name => "FileLogger";

    /// <inheritdoc />
    public async Task OnMetricsCollectedAsync(SystemMetrics metrics, CancellationToken cancellationToken = default)
    {
        var line = string.Format(
            CultureInfo.InvariantCulture,
            "{0:o}\tcpu={1:F1}\tram_used={2:F0}\tram_total={3:F0}\tdisk_used={4:F0}\tdisk_total={5:F0}",
            metrics.TimestampUtc,
            metrics.CpuPercent,
            metrics.RamUsedMb,
            metrics.RamTotalMb,
            metrics.DiskUsedMb,
            metrics.DiskTotalMb);

        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            await File.AppendAllTextAsync(_options.FilePath, line + Environment.NewLine, cancellationToken);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    private void EnsureDirectoryExists()
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(_options.FilePath));
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
            _logger.LogInformation("Created log directory '{Directory}'.", directory);
        }
    }
}
