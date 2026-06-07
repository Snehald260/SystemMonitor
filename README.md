# SystemMonitor

A cross-platform console application written in **C# (.NET 10)** that monitors system
resources (CPU, RAM, disk) in real time and forwards each reading to a set of pluggable
integrations — a console printer, a file logger, and a REST API publisher.

The application is built around a small **plugin architecture**: new behaviour can be added
by implementing a single interface and registering it, without changing the core monitoring
logic.

---

## Features

- Samples **CPU %**, **RAM (used / total)**, and **disk (used / total)** on a configurable interval.
- Prints readings to the console in a readable, aligned format.
- Ships three plugins out of the box:
  - **Console** – prints each reading.
  - **File logger** – appends each reading to a file.
  - **REST API** – `POST`s each reading as JSON to a configurable endpoint.
- Plugins are enabled/disabled and configured entirely through `appsettings.json`.
- Platform-specific collection (Windows `PerformanceCounter` + Win32 memory API) is hidden
  behind an interface, so other operating systems can be supported by adding a new collector.
- Graceful shutdown on `Ctrl+C`, structured logging, and per-plugin failure isolation.

---

## Requirements

- [.NET SDK 10](https://dotnet.microsoft.com/download) (pinned via `global.json`).

> The repository includes a local `nuget.config` that points exclusively at the public
> `nuget.org` feed, so restore works regardless of any machine-level package sources.

---

## Build & Run

From the repository root:

```powershell
# Restore and build everything
dotnet build

# Run the monitor (Ctrl+C to stop)
dotnet run --project src/SystemMonitor.App

# Run the tests
dotnet test
```

Example output:

```
Monitoring started. Interval: 5s. Active plugins: Console, FileLogger.
[14:32:31] CPU:  58.5% | RAM:  13511 / 16049 MB | Disk:  312633 / 486359 MB
[14:32:36] CPU:  35.1% | RAM:  13522 / 16049 MB | Disk:  312637 / 486359 MB
```

---

## Configuration

All settings live in [`src/SystemMonitor.App/appsettings.json`](src/SystemMonitor.App/appsettings.json):

```json
{
  "Monitor": {
    "IntervalSeconds": 5,
    "DriveRoot": null
  },
  "FileLogger": {
    "Enabled": true,
    "FilePath": "logs/metrics.log"
  },
  "RestApi": {
    "Enabled": false,
    "Endpoint": "https://example.com/metrics",
    "TimeoutSeconds": 10
  }
}
```

| Section      | Key               | Description                                                                  |
| ------------ | ----------------- | ---------------------------------------------------------------------------- |
| `Monitor`    | `IntervalSeconds` | How often to sample, in seconds (minimum 1).                                 |
| `Monitor`    | `DriveRoot`       | Drive to report disk usage for (e.g. `C:\\` or `/`). Defaults to the OS drive. |
| `FileLogger` | `Enabled`         | Register the file logging plugin.                                            |
| `FileLogger` | `FilePath`        | Output file. Relative paths resolve against the app's binary directory.      |
| `RestApi`    | `Enabled`         | Register the REST plugin.                                                     |
| `RestApi`    | `Endpoint`        | URL that readings are `POST`ed to.                                           |
| `RestApi`    | `TimeoutSeconds`  | HTTP request timeout.                                                         |

### REST payload

The REST plugin posts the exact contract required by the exercise:

```json
{ "cpu": 58.5, "ram_used": 13511, "disk_used": 312633 }
```

To try it quickly, create a throwaway endpoint (for example at <https://webhook.site>),
set `RestApi.Enabled` to `true`, paste the URL into `RestApi.Endpoint`, and run the app.

---

## Architecture

The solution follows a **clean (layered) architecture**. Dependencies point inward toward a
dependency-free `Core`, so the stable business contracts never depend on volatile details
such as the operating system or HTTP.

```
SystemMonitor.App            Composition root: host, configuration, DI wiring.
        │ depends on
SystemMonitor.Plugins        IMonitorPlugin implementations (Console, File, REST).
        │ depends on
SystemMonitor.Infrastructure Collectors (strategy) + the background MonitoringWorker.
        │ depends on
SystemMonitor.Core           Models + abstractions only. Depends on nothing.
```

### Key patterns

- **Plugin architecture** — `IMonitorPlugin` is the extension point. The
  [`MonitoringWorker`](src/SystemMonitor.Infrastructure/Monitoring/MonitoringWorker.cs)
  fans each reading out to every registered plugin. Adding a new integration (e.g. Slack)
  means writing one class and registering it — no core changes.
- **Strategy pattern** — `IMetricsCollector` abstracts *how* metrics are read. A
  Windows implementation is provided; a fallback handles other platforms. The correct
  strategy is chosen at registration time based on the OS.
- **Dependency injection** — everything is composed through the .NET Generic Host's
  container. The two `AddSystemMonitor(...)` and `AddMonitorPlugins(...)` extension methods
  keep `Program.cs` to a handful of lines.
- **Options pattern** — typed settings (`MonitorOptions`, `FileLoggerOptions`,
  `RestApiOptions`) are bound from configuration sections.

### Why this design

A monitoring tool's value grows with the integrations bolted onto it, so the most likely
axis of change is "add another output" or "support another OS." Both are isolated behind
interfaces, meaning the risky/volatile code lives at the edges while the core loop stays
small, stable, and easy to test.

---

## Project layout

```
SystemMonitor/
├── global.json                        Pins the .NET 10 SDK
├── nuget.config                       Restricts restore to nuget.org
├── SystemMonitor.sln
├── src/
│   ├── SystemMonitor.Core/            Models + abstractions
│   ├── SystemMonitor.Infrastructure/  Collectors + MonitoringWorker + DI
│   ├── SystemMonitor.Plugins/         Console, File, REST plugins + DI
│   └── SystemMonitor.App/             Host, Program.cs, appsettings.json
└── tests/
    └── SystemMonitor.Tests/           xUnit tests + test doubles
```

---

## Testing

`dotnet test` runs the xUnit suite, which covers the highest-value behaviour:

- The worker notifies **every** registered plugin with the collected metrics.
- A **failing plugin does not stop** the loop or the other plugins (failure isolation).
- The file logger writes the expected values and appends one line per cycle.
- The REST plugin sends a payload containing exactly `cpu`, `ram_used`, and `disk_used`.

Tests use small hand-written **test doubles** (a fake collector and spy/throwing plugins)
rather than a mocking framework, keeping them easy to read.

---

## Design notes, corner cases & limitations

- **CPU counter warm-up.** A Windows CPU `PerformanceCounter` always returns `0` on its first
  read because it needs a previous sample to compare against. The collector takes a throwaway
  reading at construction and waits briefly between samples so the first reported value is
  meaningful.
- **Failure isolation.** Each plugin invocation is wrapped in its own `try/catch`. A
  misbehaving plugin is logged and skipped; it can never crash the loop or starve the others.
- **Graceful shutdown.** `Ctrl+C` cancellation is treated as a normal stop signal (not an
  error), so the worker exits cleanly.
- **Deterministic log path.** Relative `FilePath` values resolve against the application's
  base (binary) directory, so logs land in the same place whether the app is launched from
  Visual Studio or `dotnet run`.
- **Interval guard.** The sampling interval is clamped to a minimum of 1 second to avoid a
  runaway busy loop from misconfiguration.
- **Cross-platform scope.** Full CPU and memory collection is implemented for **Windows**.
  On Linux/macOS the fallback collector reports disk accurately and CPU/RAM as `0` with a
  warning — a documented extension point, not a finished implementation.
- **Disk semantics.** "Used" is `TotalSize − AvailableFreeSpace` for a single configured
  drive; multi-volume aggregation is out of scope.
