using AMZN.Data.Entities;
using AMZN.DTOs.Carts;
using AMZN.Repositories.Carts;
using AMZN.Repositories.Products;
using AMZN.Repositories.Users;
using AMZN.Shared.Exceptions;
using AMZN.Shared.Exceptions.Errors;

namespace AMZN.Services.Carts;

public class CartService
{
    private readonly ICartRepository _cartRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUserRepository _userRepository;
    
    public CartService(ICartRepository cartRepository,
        IProductRepository productRepository,
        IUserRepository userRepository)
    {
        _cartRepository = cartRepository;
        _productRepository = productRepository;
        _userRepository = userRepository;
    }

    public async Task<CartResponseDto> AddToCartAsync(Guid userId, Guid productId)
    {
        User? user = await _userRepository.GetUserByIdAsync(userId);
        if (user == null)
        {
            throw new ApiException(ErrorCodes.UserNotFound, "User not found", StatusCodes.Status404NotFound);
        }
        
        Product? product = await _productRepository.GetByIdAsync(productId);
        if (product == null)
        {
            throw new ApiException(ErrorCodes.ProductNotFound, "Product not found", StatusCodes.Status404NotFound);
        }
        
        var cart = await _cartRepository.GetCartByUserIdAsync(user.Id);
        if (cart == null)
        {
            cart = new() 
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                User = user,
                Items = new List<CartItem>()
            };
            cart.Items.Add(new CartItem
            {
                Id = Guid.NewGuid(),
                CartId = cart.Id,
                ProductId = productId,
                Product = product,
                Cart = cart,
                Quantity = 1
            });
            await _cartRepository.AddCartAsync(cart);
        }
        else
        {
            var item = cart.Items.FirstOrDefault(x =>
            {
                return x.ProductId == productId;
            });
            if (item == null)
            {
                CartItem cartItem = new()
                {
                    Id = Guid.NewGuid(),
                    CartId = cart.Id,
                    ProductId = productId,
                    Product = product,
                    Cart = cart,
                    Quantity = 1
                };
                await _cartRepository.AddCartItemAsync(cartItem);
            }
            else
            {
                item.Quantity++;
                await _cartRepository.SaveChangesAsync();
            }
        }
        List<CartItemDto> items = new List<CartItemDto>();
        foreach (var ci in cart.Items)
        {
            items.Add(new CartItemDto()
            {
                ProductId = ci.ProductId,
                ImageUrl = ci.Product.PrimaryImageUrl,
                Title = ci.Product.Title,
                Quantity = ci.Quantity,
                Price = ci.Product.CurrentPrice
            });
        }
        return new CartResponseDto()
        {
            CartId = cart.Id,
            Items = items
        };
    }

    public async Task<CartResponseDto> RemoveFromCartAsync(Guid userId, Guid productId)
    {
        var user = await _userRepository.GetUserByIdAsync(userId);
        if (user == null)
        {
            throw new ApiException(ErrorCodes.UserNotFound, "User not found", StatusCodes.Status404NotFound);
        }
        
        Product? product = await _productRepository.GetByIdAsync(productId);
        if (product == null)
        {
            throw new ApiException(ErrorCodes.ProductNotFound, "Product not found", StatusCodes.Status404NotFound);
        }
        
        var cart = await _cartRepository.GetCartByUserIdAsync(user.Id);
        if (cart == null)
        {
            throw new ApiException(ErrorCodes.CartNotFound, "Cart not found", StatusCodes.Status404NotFound);
        }
        
        var item = cart.Items.FirstOrDefault(x =>
        {
            return x.ProductId == productId;
        });
        if (item == null)
        {
            throw new ApiException(ErrorCodes.ProductNotFound, "Product not in cart", StatusCodes.Status404NotFound);
        }

        await _cartRepository.RemoveCartItemAsync(item);
        
        List<CartItemDto> items = new List<CartItemDto>();
        if (cart.Items.Count > 0)
        {
            foreach (var ci in cart.Items)
            {
                items.Add(new CartItemDto()
                {
                    ProductId = ci.ProductId,
                    ImageUrl = ci.Product.PrimaryImageUrl,
                    Title = ci.Product.Title,
                    Quantity = ci.Quantity,
                    Price = ci.Product.CurrentPrice
                });
            }
        }

        return new CartResponseDto()
        {
            CartId = cart.Id,
            Items = items
        };
    }

    public async Task<CartResponseDto> ClearCartAsync(Guid userId)
    {
        var user = await _userRepository.GetUserByIdAsync(userId);
        if (user == null)
        {
            throw new ApiException(ErrorCodes.UserNotFound, "User not found", StatusCodes.Status404NotFound);
        }
        
        var cart = await _cartRepository.GetCartByUserIdAsync(user.Id, includeItems: false);
        if (cart == null)
        {
            throw new ApiException(ErrorCodes.CartNotFound, "Cart not found", StatusCodes.Status404NotFound);
        }

        if (await _cartRepository.ClearCartAsync(cart.Id) == 0)
        {
            throw new ApiException(ErrorCodes.ProductNotFound, "Cart is empty", StatusCodes.Status404NotFound);
        }

        List<CartItemDto> items = new List<CartItemDto>();
        
        return new CartResponseDto()
        {
            CartId = cart.Id,
            Items = items,
        };
    }
    
    public async Task<CartResponseDto> GetCartAsync(Guid userId)
    {
        User? user = await _userRepository.GetUserByIdAsync(userId);
        if (user == null)
        {
            throw new ApiException(ErrorCodes.UserNotFound, "User not found", StatusCodes.Status404NotFound);
        }
        
        Cart? cart = await _cartRepository.GetCartByUserIdAsync(userId);
        if (cart == null)
        {
            throw new ApiException(ErrorCodes.CartNotFound, "Cart not found", StatusCodes.Status404NotFound);
        }
        
        List<CartItemDto> items = new List<CartItemDto>();
        if (cart.Items.Count > 0)
        {
            foreach (CartItem ci in cart.Items)
            {
                CartItemDto item = new()
                {
                    ProductId = ci.ProductId,
                    ImageUrl = ci.Product.PrimaryImageUrl,
                    Title = ci.Product.Title,
                    Quantity = ci.Quantity,
                    Price = ci.Product.CurrentPrice
                };
                items.Add(item);
            }
        }

        return new CartResponseDto()
        {
            CartId = cart.Id,
            Items = items
        };     
    }

    public async Task<CartResponseDto> IncreaseQuantityAsync(Guid userId, Guid productId)
    {
        var user = await _userRepository.GetUserByIdAsync(userId);
        if (user == null)
        {
            throw new ApiException(ErrorCodes.UserNotFound, "User not found", StatusCodes.Status404NotFound);
        }
        
        var product = await _productRepository.GetByIdAsync(productId);
        if (product == null)
        {
            throw new ApiException(ErrorCodes.ProductNotFound, "Product not found", StatusCodes.Status404NotFound);
        }
        
        var cart = await _cartRepository.GetCartByUserIdAsync(user.Id);
        if (cart == null)
        {
            throw new ApiException(ErrorCodes.CartNotFound, "Cart not found", StatusCodes.Status404NotFound);
        }
        
        var item = cart.Items.FirstOrDefault(x =>
        {
            return x.ProductId == productId;
        });
        if (item == null)
        {
            throw new ApiException(ErrorCodes.ProductNotFound, "Product not in cart", StatusCodes.Status404NotFound);
        }
        
        item.Quantity++;
        await _cartRepository.SaveChangesAsync();
        
        List<CartItemDto> items = new List<CartItemDto>();
        foreach (var ci in cart.Items)
        {
            items.Add(new CartItemDto()
            {
                ProductId = ci.ProductId,
                ImageUrl = ci.Product.PrimaryImageUrl,
                Title = ci.Product.Title,
                Quantity = ci.Quantity,
                Price = ci.Product.CurrentPrice
            });
        }
        return new CartResponseDto()
        {
            CartId = cart.Id,
            Items = items
        };  
    }

    public async Task<CartResponseDto> DecreaseQuantityAsync(Guid userId, Guid productId)
    {
        var user = await _userRepository.GetUserByIdAsync(userId);
        if (user == null)
        {
            throw new ApiException(ErrorCodes.UserNotFound, "User not found", StatusCodes.Status404NotFound);
        }
        
        var product = await _productRepository.GetByIdAsync(productId);
        if (product == null)
        {
            throw new ApiException(ErrorCodes.ProductNotFound, "Product not found", StatusCodes.Status404NotFound);
        }
        
        var cart = await _cartRepository.GetCartByUserIdAsync(user.Id);
        if (cart == null)
        {
            throw new ApiException(ErrorCodes.CartNotFound, "Cart not found", StatusCodes.Status404NotFound);
        }
        
        var item = cart.Items.FirstOrDefault(x =>
        {
            return x.ProductId == productId;
        });
        if (item == null)
        {
            throw new ApiException(ErrorCodes.ProductNotFound, "Product not in cart", StatusCodes.Status404NotFound);
        }
        
        item.Quantity--;
        if (item.Quantity <= 0)
        {
            await _cartRepository.RemoveCartItemAsync(item);
        }
        else
        {
            await _cartRepository.SaveChangesAsync();
        }
        
        List<CartItemDto> items = new List<CartItemDto>();
        if (cart.Items.Count > 0)
        {
            foreach (var ci in cart.Items)
            {
                items.Add(new CartItemDto()
                {
                    ProductId = ci.ProductId,
                    ImageUrl = ci.Product.PrimaryImageUrl,
                    Title = ci.Product.Title,
                    Quantity = ci.Quantity,
                    Price = ci.Product.CurrentPrice
                });
            }
        }

        return new CartResponseDto()
        {
            CartId = cart.Id,
            Items = items
        };
    }

    public async Task CreateCartsIfNotExist()
    {
        var users = _cartRepository.GetUsersWithoutCartAsync().Result;
        if (users == null)
        {
            return;
        }
        foreach (var user in users)
        {
           Cart cart = new()
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                User = user,
                Items = new List<CartItem>()
            };
            await _cartRepository.AddCartAsync(cart);
        }
    }
    
}
