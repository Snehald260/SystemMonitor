using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SystemMonitor.Core.Models;
using SystemMonitor.Plugins;
using SystemMonitor.Plugins.Configuration;

namespace SystemMonitor.Tests;

public class RestApiPluginTests
{
    [Fact]
    public async Task Posts_payload_with_the_required_json_keys()
    {
        var handler = new CapturingHandler();
        var httpClient = new HttpClient(handler);
        var options = Options.Create(new RestApiOptions
        {
            Enabled = true,
            Endpoint = "https://example.test/metrics"
        });

        var plugin = new RestApiPlugin(httpClient, options, NullLogger<RestApiPlugin>.Instance);

        var metrics = new SystemMetrics(
            CpuPercent: 33.0,
            RamUsedMb: 2048,
            RamTotalMb: 8192,
            DiskUsedMb: 777,
            DiskTotalMb: 1000,
            TimestampUtc: DateTimeOffset.UnixEpoch);

        await plugin.OnMetricsCollectedAsync(metrics);

        Assert.NotNull(handler.LastBody);
        Assert.Contains("\"cpu\"", handler.LastBody);
        Assert.Contains("\"ram_used\"", handler.LastBody);
        Assert.Contains("\"disk_used\"", handler.LastBody);
        // Values that must NOT be present (the payload only carries the required three).
        Assert.DoesNotContain("ram_total", handler.LastBody);
        Assert.DoesNotContain("disk_total", handler.LastBody);
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public string? LastBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Content is not null)
            {
                LastBody = await request.Content.ReadAsStringAsync(cancellationToken);
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
