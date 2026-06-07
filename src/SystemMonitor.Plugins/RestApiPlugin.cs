using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SystemMonitor.Core.Abstractions;
using SystemMonitor.Core.Models;
using SystemMonitor.Plugins.Configuration;

namespace SystemMonitor.Plugins;

/// <summary>
/// A plugin that POSTs each metrics snapshot to a configurable REST endpoint as JSON.
/// </summary>
/// <remarks>
/// The payload matches the required contract exactly:
/// <c>{"cpu": &lt;percent&gt;, "ram_used": &lt;mb&gt;, "disk_used": &lt;mb&gt;}</c>.
/// </remarks>
public sealed class RestApiPlugin : IMonitorPlugin
{
    private readonly HttpClient _httpClient;
    private readonly RestApiOptions _options;
    private readonly ILogger<RestApiPlugin> _logger;

    public RestApiPlugin(HttpClient httpClient, IOptions<RestApiOptions> options, ILogger<RestApiPlugin> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => "RestApi";

    /// <inheritdoc />
    public async Task OnMetricsCollectedAsync(SystemMetrics metrics, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.Endpoint))
        {
            _logger.LogWarning("RestApi endpoint is not configured; skipping POST.");
            return;
        }

        var payload = new MetricsPayload(metrics.CpuPercent, metrics.RamUsedMb, metrics.DiskUsedMb);

        try
        {
            using var response = await _httpClient.PostAsJsonAsync(_options.Endpoint, payload, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "POST to {Endpoint} returned {StatusCode}.", _options.Endpoint, (int)response.StatusCode);
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to POST metrics to {Endpoint}.", _options.Endpoint);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            // Triggered by the HttpClient timeout rather than application shutdown.
            _logger.LogError(ex, "POST to {Endpoint} timed out.", _options.Endpoint);
        }
    }

    /// <summary>
    /// The exact JSON shape required by the exercise. Property names are lower_snake_case.
    /// </summary>
    private sealed record MetricsPayload(
        [property: System.Text.Json.Serialization.JsonPropertyName("cpu")] double Cpu,
        [property: System.Text.Json.Serialization.JsonPropertyName("ram_used")] double RamUsed,
        [property: System.Text.Json.Serialization.JsonPropertyName("disk_used")] double DiskUsed);
}
