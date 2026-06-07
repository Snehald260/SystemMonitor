using Microsoft.Extensions.Hosting;
using SystemMonitor.Infrastructure;
using SystemMonitor.Plugins;

// Build the application host. The host wires up configuration, logging, dependency
// injection and graceful shutdown (Ctrl+C) for us.
var builder = Host.CreateApplicationBuilder(args);

// Register the monitoring infrastructure (collector strategy + background worker)...
builder.Services.AddSystemMonitor(builder.Configuration);

// ...and the plugins (console always on; file and REST when enabled in configuration).
builder.Services.AddMonitorPlugins(builder.Configuration);

var host = builder.Build();

// Runs until Ctrl+C / SIGTERM, then shuts the worker down gracefully.
await host.RunAsync();
