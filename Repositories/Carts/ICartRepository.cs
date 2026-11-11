using AMZN.Data.Entities;

namespace AMZN.Repositories.Carts;

public interface ICartRepository
{
    Task AddCartAsync(Cart cart);
    
    Task<Cart?> GetCartAsync(Guid cartId);
    
    Task<bool> IsCartExistsAsync(Guid cartId);
    
    Task<Cart?> GetCartByUserIdAsync(Guid userId, bool includeItems = true);
    Task<bool> IsCartEmptyAsync(Guid cartId);
    
    Task AddCartItemAsync(CartItem cartItem);
    
    Task RemoveCartItemAsync(CartItem cartItem);
    
    Task DeleteCartAsync(Cart cart);
    
    Task<List<CartItem>?> GetCartItemsAsync(Guid cartId);
    
    Task<CartItem?> IsItemInCartAsync(Guid productId, Guid cartId);

    Task SaveChangesAsync();
    
    Task<List<User>?> GetUsersWithoutCartAsync();
    
    Task<int> ClearCartAsync(Guid cartId);

}
