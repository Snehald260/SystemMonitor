namespace SystemMonitor.Infrastructure.Configuration;

/// <summary>
/// Strongly-typed options that control the monitoring loop.
/// Bound from the "Monitor" section of configuration (for example appsettings.json).
/// </summary>
public sealed class MonitorOptions
{
    /// <summary>
    /// The configuration section name these options are bound from.
    /// </summary>
    public const string SectionName = "Monitor";

    /// <summary>
    /// How often, in seconds, to sample system resources. Defaults to 5 seconds.
    /// </summary>
    public int IntervalSeconds { get; set; } = 5;

    /// <summary>
    /// The drive whose disk usage should be reported (for example "C:\\" on Windows
    /// or "/" on Linux/macOS). When null or empty, the drive that hosts the operating
    /// system is used.
    /// </summary>
    public string? DriveRoot { get; set; }
}
