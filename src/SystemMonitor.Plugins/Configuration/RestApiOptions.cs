namespace SystemMonitor.Plugins.Configuration;

/// <summary>
/// Options for the REST API plugin, bound from the "RestApi" configuration section.
/// </summary>
public sealed class RestApiOptions
{
    /// <summary>
    /// The configuration section name these options are bound from.
    /// </summary>
    public const string SectionName = "RestApi";

    /// <summary>
    /// When false, the plugin is not registered and posts nothing.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// The absolute URL that metrics are POSTed to as JSON.
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Maximum time, in seconds, to wait for the HTTP request before timing out.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 10;
}
