namespace SystemMonitor.Plugins.Configuration;

/// <summary>
/// Options for the file logging plugin, bound from the "FileLogger" configuration section.
/// </summary>
public sealed class FileLoggerOptions
{
    /// <summary>
    /// The configuration section name these options are bound from.
    /// </summary>
    public const string SectionName = "FileLogger";

    /// <summary>
    /// When false, the plugin is not registered and writes nothing.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Path of the file that metrics are appended to. Relative paths are resolved against
    /// the application's working directory. Defaults to "logs/metrics.log".
    /// </summary>
    public string FilePath { get; set; } = Path.Combine("logs", "metrics.log");
}
