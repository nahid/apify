using Apify.Models;
using Apify.Utils;

namespace Apify.Services;

public class CacheService
{
    private const string CacheFileName = "cache";
    
    private CacheSchema _cache = new CacheSchema();
    private string _cacheFilePath;

    public CacheService()
    {
        _cacheFilePath = MiscHelper.GetSystemConfigPath(CacheFileName);
        
        var cachePath = GetCacheFilePath();

        if (!File.Exists(cachePath))
        {
            SaveCache();
        }
        
    }
    

    private string GetCacheFilePath()
    {
        return _cacheFilePath;
    }

    public CacheSchema LoadCache()
    {
        
        var cachePath = GetCacheFilePath();

        if (!File.Exists(cachePath))
        {
            SaveCache();
        }

        string json = File.ReadAllText(cachePath);
        _cache = JsonHelper.DeserializeString<CacheSchema>(json) ?? new CacheSchema();
        
        return _cache;
    }
    
    public void SaveCache()
    {
        if (_cache == null)
        {
            throw new InvalidOperationException("Cache is not loaded. Call LoadCache() first.");
        }

        string cachePath = GetCacheFilePath();
        string json = JsonHelper.SerializeToJson(_cache);
        File.WriteAllText(cachePath, json);
    }
    
    public void UpdateVersion(string version)
    {
        LoadCache();

        _cache.Version = new VersionCache(version);
        SaveCache();
        
    }
    
    public VersionCache? GetVersionCache()
    {
        LoadCache();
        
        return _cache.Version;
    }
}