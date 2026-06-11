using Microsoft.Extensions.Caching.Memory;

namespace UltimateMonopoly.Services.Cache;

public class CardCacheService
{
    private readonly IMemoryCache _memoryCache;
    private const string CacheKey = "Boards";
    private static readonly TimeSpan CustomBoardExpiration = TimeSpan.FromHours(6);

    public CardCacheService(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }
    
    private string GetKey(string cardId)
        => $"{CacheKey}__{cardId}";
}