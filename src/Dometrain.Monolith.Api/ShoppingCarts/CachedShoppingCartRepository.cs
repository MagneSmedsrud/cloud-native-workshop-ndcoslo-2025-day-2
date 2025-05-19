using System.Text.Json;
using StackExchange.Redis;

namespace Dometrain.Monolith.Api.ShoppingCarts;

public class CachedShoppingCartRepository : IShoppingCartRepository
{
    private readonly IShoppingCartRepository _shoppingCartRepository;
    private readonly IConnectionMultiplexer _connectionMultiplexer;

    public CachedShoppingCartRepository(
        IShoppingCartRepository shoppingCartRepository, 
        IConnectionMultiplexer connectionMultiplexer)
    {
        _shoppingCartRepository = shoppingCartRepository;
        _connectionMultiplexer = connectionMultiplexer;
    }

    public async Task<bool> AddCourseAsync(Guid studentId, Guid courseId)
    {
        var created = await _shoppingCartRepository.AddCourseAsync(studentId, courseId);
        if (created)
        {
            var db = _connectionMultiplexer.GetDatabase();
            await db.KeyDeleteAsync($"cart_id_{studentId}");
        }

        return created;
    }

    public async Task<ShoppingCart?> GetByIdAsync(Guid studentId)
    {
        var db = _connectionMultiplexer.GetDatabase();
        var cachedCartString = await db.StringGetAsync($"cart_id_{studentId}");
        if (!cachedCartString.IsNull)
        {
            return JsonSerializer.Deserialize<ShoppingCart>(cachedCartString.ToString());
        }
        
        var cart = await _shoppingCartRepository.GetByIdAsync(studentId);
        await db.StringSetAsync($"cart_id_{studentId}", JsonSerializer.Serialize(cart));
        return cart;
    }

    public async Task<bool> RemoveItemAsync(Guid studentId, Guid courseId)
    {
        var removed = await _shoppingCartRepository.RemoveItemAsync(studentId, courseId);
        if (removed)
        {
            var db = _connectionMultiplexer.GetDatabase();
            await db.KeyDeleteAsync($"cart_id_{studentId}");
        }

        return removed;
    }

    public async Task<bool> ClearAsync(Guid studentId)
    {
        var cleared = await _shoppingCartRepository.ClearAsync(studentId);
        if (cleared)
        {
            var db = _connectionMultiplexer.GetDatabase();
            await db.KeyDeleteAsync($"cart_id_{studentId}");
        }

        return cleared;
    }
}
