using Microsoft.Extensions.Caching.Memory;

namespace ProjectManagement.Shared.Infrastructure.Services;

public class MemoryCacheService : ICacheService
{
    private static readonly TimeSpan DefaultExpiry = TimeSpan.FromMinutes(5);
    private readonly IMemoryCache _cache;

    public MemoryCacheService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public T? Get<T>(string key) => _cache.TryGetValue(key, out T? value) ? value : default;

    public void Set<T>(string key, T value, TimeSpan? expiry = null)
    {
        _cache.Set(key, value, expiry ?? DefaultExpiry);
    }

    public void Remove(string key) => _cache.Remove(key);
}
