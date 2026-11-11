using AMZN.Data;
using AMZN.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AMZN.Repositories.Carts;

public class CartRepository : ICartRepository
{
    
    private readonly DataContext _dataContext;

    public CartRepository(DataContext dataContext)
    {
        _dataContext = dataContext;
    }
    public async Task AddCartAsync(Cart cart)
    {
        await _dataContext.Carts.AddAsync(cart);
        await _dataContext.SaveChangesAsync();
    }

    public async Task<Cart?> GetCartAsync(Guid cartId)
    {
        return await _dataContext.Carts.FirstOrDefaultAsync(c => c.Id == cartId);
    }
    
    public async Task<bool> IsCartExistsAsync(Guid cartId)
    {
        return await _dataContext.Carts.AnyAsync(c => c.Id == cartId);
    }
    
    public Task<Cart?> GetCartByUserIdAsync(Guid userId, bool includeItems = true)
    {
        IQueryable<Cart> query = _dataContext.Carts;
        if (includeItems)
            query = query.Include(c => c.Items).ThenInclude(i => i.Product).ThenInclude(p => p.Brand);

        return query.FirstOrDefaultAsync(c => c.UserId == userId);
    }
    
    public async Task<bool> IsCartEmptyAsync(Guid cartId)
    {
        return !(await _dataContext.CartItems.AnyAsync(ci => ci.CartId == cartId));
    }
    
    public async Task AddCartItemAsync(CartItem cartItem)
    {
        // EF добавит позицию в cart.Items через связь с уже загруженной корзиной.
        await _dataContext.CartItems.AddAsync(cartItem);
        await _dataContext.SaveChangesAsync();
    }
    
    public async Task RemoveCartItemAsync(CartItem cartItem)
    {
        cartItem.Cart.Items.Remove(cartItem);
        _dataContext.CartItems.Remove(cartItem);
        await _dataContext.SaveChangesAsync();
    }
    
    public async Task DeleteCartAsync(Cart cart)
    {
        _dataContext.Carts.Remove(cart);
        await _dataContext.SaveChangesAsync();
    }
    
    public async Task<List<CartItem>?> GetCartItemsAsync(Guid cartId)
    {
        return await _dataContext.CartItems
            .Include(p => p.Product)
            .Where(ci => ci.CartId == cartId).ToListAsync();
    }
    
    public async Task<CartItem?> IsItemInCartAsync(Guid productId, Guid cartId)
    {
        return await _dataContext.CartItems
            .Include(p => p.Product)
            .FirstOrDefaultAsync(ci => ci.ProductId == productId && ci.CartId == cartId);
    }
    
    public Task SaveChangesAsync()
    {
        return _dataContext.SaveChangesAsync();
    }

    public async Task<List<User>?> GetUsersWithoutCartAsync()
    {
        List<User> users = new List<User>();
        List<Guid> userId = await _dataContext.Carts.Select(c => c.UserId).ToListAsync();
        foreach (var user in _dataContext.Users)
        {
            if (!userId.Contains(user.Id))
            {
                users.Add(user);
            }
        }
        return users;
    }

    // ExecuteDelete удаляет строки сразу в SQL и не обновляет уже загруженные cart.Items.
    public Task<int> ClearCartAsync(Guid cartId)
    {
        return _dataContext.CartItems.Where(ci => ci.CartId == cartId).ExecuteDeleteAsync();
    }
}
