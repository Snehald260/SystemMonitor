using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SystemMonitor.Core.Abstractions;
using SystemMonitor.Core.Models;
using SystemMonitor.Infrastructure.Configuration;

namespace SystemMonitor.Infrastructure.Monitoring;

/// <summary>
/// The background service that drives the monitoring loop. On a fixed interval it asks the
/// configured <see cref="IMetricsCollector"/> for a snapshot and forwards it to every
/// registered <see cref="IMonitorPlugin"/>.
/// </summary>
/// <remarks>
/// Each plugin call is wrapped in its own try/catch so that one misbehaving plugin can never
/// stop the loop or prevent the other plugins from receiving data.
/// </remarks>
public sealed class MonitoringWorker : BackgroundService
{
    private readonly IMetricsCollector _collector;
    private readonly IReadOnlyList<IMonitorPlugin> _plugins;
    private readonly MonitorOptions _options;
    private readonly ILogger<MonitoringWorker> _logger;

    public MonitoringWorker(
        IMetricsCollector collector,
        IEnumerable<IMonitorPlugin> plugins,
        IOptions<MonitorOptions> options,
        ILogger<MonitoringWorker> logger)
    {
        _collector = collector;
        _plugins = plugins.ToList();
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(1, _options.IntervalSeconds));
        using var timer = new PeriodicTimer(interval);

        _logger.LogInformation(
            "Monitoring started. Interval: {IntervalSeconds}s. Active plugins: {Plugins}.",
            interval.TotalSeconds,
            _plugins.Count == 0 ? "(none)" : string.Join(", ", _plugins.Select(p => p.Name)));

        try
        {
            do
            {
                await RunCycleAsync(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            // Expected when the application is shutting down.
        }

        _logger.LogInformation("Monitoring stopped.");
    }

    private async Task RunCycleAsync(CancellationToken cancellationToken)
    {
        SystemMetrics metrics;
        try
        {
            metrics = await _collector.CollectAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw; // Let cancellation bubble up to stop the loop.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to collect system metrics for this cycle.");
            return;
        }

        await DispatchAsync(metrics, cancellationToken);
    }

    private async Task DispatchAsync(SystemMetrics metrics, CancellationToken cancellationToken)
    {
        foreach (var plugin in _plugins)
        {
            try
            {
                await plugin.OnMetricsCollectedAsync(metrics, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw; // Propagate shutdown.
            }
            catch (Exception ex)
            {
                // Isolate plugin failures: log and continue with the remaining plugins.
                _logger.LogError(ex, "Plugin '{Plugin}' failed while processing metrics.", plugin.Name);
            }
        }
    }
}
