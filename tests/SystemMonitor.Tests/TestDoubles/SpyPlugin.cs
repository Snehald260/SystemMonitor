using SystemMonitor.Core.Abstractions;
using SystemMonitor.Core.Models;

namespace SystemMonitor.Tests.TestDoubles;

/// <summary>
/// A plugin that records every metrics snapshot it receives and signals when it has been
/// called, so tests can wait deterministically instead of relying on fixed delays.
/// </summary>
internal sealed class SpyPlugin : IMonitorPlugin
{
    private readonly TaskCompletionSource _firstCall =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public SpyPlugin(string name = "Spy")
    {
        Name = name;
    }

    public string Name { get; }

    public List<SystemMetrics> Received { get; } = [];

    /// <summary>A task that completes the first time the plugin is invoked.</summary>
    public Task FirstCall => _firstCall.Task;

    public Task OnMetricsCollectedAsync(SystemMetrics metrics, CancellationToken cancellationToken = default)
    {
        Received.Add(metrics);
        _firstCall.TrySetResult();
        return Task.CompletedTask;
    }
}
