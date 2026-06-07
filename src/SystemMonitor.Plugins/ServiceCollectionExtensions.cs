using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SystemMonitor.Core.Abstractions;
using SystemMonitor.Plugins.Configuration;

namespace SystemMonitor.Plugins;

/// <summary>
/// Registration helpers that wire the monitoring plugins into the dependency injection
/// container. Each plugin is only registered when it is enabled in configuration, so the
/// set of active plugins can be changed without touching code.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the console plugin (always on) plus the file and REST plugins when they are
    /// enabled in configuration.
    /// </summary>
    /// <param name="services">The service collection to add registrations to.</param>
    /// <param name="configuration">Application configuration used to bind plugin options.</param>
    /// <returns>The same <paramref name="services"/> instance for chaining.</returns>
    public static IServiceCollection AddMonitorPlugins(this IServiceCollection services, IConfiguration configuration)
    {
        // The console plugin has no configuration and is always available.
        services.AddSingleton<IMonitorPlugin, ConsolePlugin>();

        AddFileLogger(services, configuration);
        AddRestApi(services, configuration);

        return services;
    }

    private static void AddFileLogger(IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(FileLoggerOptions.SectionName);
        services.Configure<FileLoggerOptions>(section);

        var options = section.Get<FileLoggerOptions>() ?? new FileLoggerOptions();
        if (options.Enabled)
        {
            services.AddSingleton<IMonitorPlugin, FileLoggerPlugin>();
        }
    }

    private static void AddRestApi(IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(RestApiOptions.SectionName);
        services.Configure<RestApiOptions>(section);

        var options = section.Get<RestApiOptions>() ?? new RestApiOptions();
        if (!options.Enabled)
        {
            return;
        }

        // Register as a typed HttpClient so the factory manages the client's lifetime.
        services.AddHttpClient<IMonitorPlugin, RestApiPlugin>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(Math.Max(1, options.TimeoutSeconds));
        });
    }
}
