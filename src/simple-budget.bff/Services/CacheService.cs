using Microsoft.Extensions.Caching.Memory;

namespace simple_budget.bff;

public class CacheService : ICacheService
{
    private readonly IMemoryCache _cache;

    public CacheService(IMemoryCache cache)
    {
        _cache = cache;
    }
    public T? GetEntry<T>(string key)
    {
        _cache.TryGetValue(key, out T? item);
        
        return item;
    }

    public T SetEntry<T>(string key, T item)
    {
        _cache.CreateEntry(key);
        _cache.Set(key, item);

        return item;
    }
}
