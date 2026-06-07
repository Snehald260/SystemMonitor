using SystemMonitor.Infrastructure.Configuration;

namespace SystemMonitor.Infrastructure.Collectors;

/// <summary>
/// Reads disk usage for a drive. Disk information is available identically on every
/// platform that .NET supports, so this logic is shared by all collectors.
/// </summary>
internal static class DiskUsageReader
{
    private const double BytesPerMb = 1024d * 1024d;

    /// <summary>
    /// Returns the used and total size, in megabytes, of the configured drive.
    /// </summary>
    /// <param name="options">Monitoring options that may specify which drive to read.</param>
    /// <returns>A tuple of (usedMb, totalMb). Returns (0, 0) if the drive is unavailable.</returns>
    public static (double UsedMb, double TotalMb) Read(MonitorOptions options)
    {
        var root = ResolveDriveRoot(options.DriveRoot);
        var drive = new DriveInfo(root);

        if (!drive.IsReady)
        {
            return (0d, 0d);
        }

        var totalMb = drive.TotalSize / BytesPerMb;
        var usedMb = (drive.TotalSize - drive.AvailableFreeSpace) / BytesPerMb;
        return (usedMb, totalMb);
    }

    private static string ResolveDriveRoot(string? configuredRoot)
    {
        if (!string.IsNullOrWhiteSpace(configuredRoot))
        {
            return configuredRoot;
        }

        // Fall back to the drive that hosts the operating system.
        return Path.GetPathRoot(Environment.SystemDirectory) ?? Path.GetPathRoot(Environment.CurrentDirectory) ?? "/";
    }
}
