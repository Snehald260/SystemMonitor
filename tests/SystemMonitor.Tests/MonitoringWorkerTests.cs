using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SystemMonitor.Core.Abstractions;
using SystemMonitor.Infrastructure.Configuration;
using SystemMonitor.Infrastructure.Monitoring;
using SystemMonitor.Tests.TestDoubles;

namespace SystemMonitor.Tests;

public class MonitoringWorkerTests
{
    private static MonitoringWorker CreateWorker(IMetricsCollector collector, IEnumerable<IMonitorPlugin> plugins)
    {
        var options = Options.Create(new MonitorOptions { IntervalSeconds = 1 });
        return new MonitoringWorker(collector, plugins, options, NullLogger<MonitoringWorker>.Instance);
    }

    [Fact]
    public async Task Notifies_all_registered_plugins_with_collected_metrics()
    {
        var collector = new FakeMetricsCollector();
        var pluginA = new SpyPlugin("A");
        var pluginB = new SpyPlugin("B");
        var worker = CreateWorker(collector, [pluginA, pluginB]);

        await worker.StartAsync(CancellationToken.None);
        await Task.WhenAll(pluginA.FirstCall, pluginB.FirstCall).WaitAsync(TimeSpan.FromSeconds(5));
        await worker.StopAsync(CancellationToken.None);

        Assert.NotEmpty(pluginA.Received);
        Assert.NotEmpty(pluginB.Received);
        Assert.Equal(12.5, pluginA.Received[0].CpuPercent);
        Assert.Equal(4096, pluginB.Received[0].RamUsedMb);
    }

    [Fact]
    public async Task A_failing_plugin_does_not_prevent_other_plugins_from_running()
    {
        var collector = new FakeMetricsCollector();
        var healthy = new SpyPlugin("Healthy");

        // The throwing plugin is listed first to prove the loop continues past a failure.
        var worker = CreateWorker(collector, [new ThrowingPlugin(), healthy]);

        await worker.StartAsync(CancellationToken.None);
        await healthy.FirstCall.WaitAsync(TimeSpan.FromSeconds(5));
        await worker.StopAsync(CancellationToken.None);

        Assert.NotEmpty(healthy.Received);
    }
}
