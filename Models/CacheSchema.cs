namespace Apify.Models;

public class CacheSchema
{
    public VersionCache? Version { get; set; }
}


public class VersionCache
{
    public string LatestVersion { get; set; } = string.Empty;
    
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    public VersionCache() { }

    public VersionCache(string version)
    {
        LatestVersion = version;
        LastUpdated = DateTime.UtcNow;
    }
}