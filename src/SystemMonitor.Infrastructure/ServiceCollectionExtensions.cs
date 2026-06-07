using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SystemMonitor.Core.Abstractions;
using SystemMonitor.Infrastructure.Collectors;
using SystemMonitor.Infrastructure.Configuration;
using SystemMonitor.Infrastructure.Monitoring;

namespace SystemMonitor.Infrastructure;

/// <summary>
/// Registration helpers that wire the monitoring infrastructure into the dependency
/// injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the monitoring options, the platform-appropriate metrics collector, and the
    /// background monitoring worker.
    /// </summary>
    /// <param name="services">The service collection to add registrations to.</param>
    /// <param name="configuration">Application configuration used to bind options.</param>
    /// <returns>The same <paramref name="services"/> instance for chaining.</returns>
    public static IServiceCollection AddSystemMonitor(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MonitorOptions>(configuration.GetSection(MonitorOptions.SectionName));

        // Select the collector strategy based on the current operating system.
        if (OperatingSystem.IsWindows())
        {
            services.AddSingleton<IMetricsCollector, WindowsMetricsCollector>();
        }
        else
        {
            services.AddSingleton<IMetricsCollector, FallbackMetricsCollector>();
        }

        services.AddHostedService<MonitoringWorker>();

        return services;
    }
}
