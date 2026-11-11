using AMZN.Data.Entities;
using AMZN.DTOs.Orders;
using AMZN.Repositories.Carts;
using AMZN.Repositories.Orders;
using AMZN.Repositories.Products;
using AMZN.Repositories.Users;
using AMZN.Shared.Exceptions;
using AMZN.Shared.Exceptions.Errors;
using AMZN.Shared.Transactions;

namespace AMZN.Services.Orders;

public class OrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICartRepository _cartRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUserRepository _userRepository;
    private readonly ITransactionManager _transactionManager;

    public OrderService(IOrderRepository orderRepository,
        ICartRepository cartRepository,
        IProductRepository productRepository,
        IUserRepository userRepository,
        ITransactionManager transactionManager)
    {
        _orderRepository = orderRepository;
        _cartRepository = cartRepository;
        _productRepository = productRepository;
        _userRepository = userRepository;
        _transactionManager = transactionManager;
    }

    public Task<OrderDto> CreateOrderAsync(Guid userId)
    {
        return _transactionManager.ExecuteAsync(() =>
        {
            return CreateOrderCoreAsync(userId);
        });
    }

    private async Task<OrderDto> CreateOrderCoreAsync(Guid userId)
    {
        var user = await LockUserForOrderChangeAsync(userId);

        var addresses = await _userRepository.GetUserDeliveryAddressesAsync(user);
        if (addresses == null)
        {
            throw new ApiException(ErrorCodes.UserNotFound, "User not found", StatusCodes.Status404NotFound);
        }
        
        var paymentMethods = await _userRepository.GetUserPaymentMethodsAsync(user);
        if (paymentMethods == null)
        {
            throw new ApiException(ErrorCodes.UserNotFound, "User not found", StatusCodes.Status404NotFound);
        }
        
        var cart = await ValidateCartAsync(userId);

        var order = await _orderRepository.GetPendingOrderByUserIdAsync(user.Id);
        if (order != null)
        {
            throw new ApiException(ErrorCodes.PendingOrderExists, "Pending order exists", StatusCodes.Status409Conflict);
        }
        
        int quantity = cart.Items.Sum(item =>
        {
            return item.Quantity;
        });
        decimal totalPrice = cart.Items.Sum(item =>
        {
            return item.Product.CurrentPrice * item.Quantity;
        });

        if (quantity == 0)
        {
            throw new ApiException(ErrorCodes.CartNotFound, "Cart is empty", StatusCodes.Status404NotFound);
        }

        order = new()
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TotalAmount = totalPrice,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.Now,
            User = user,
        };
        order.OrderItems = BuildOrderItems(cart, order);
        await _orderRepository.AddOrderAsync(order);

        return new OrderDto()
        {
            OrderId = order.Id,
            TotalQuantity = quantity,
            TotalAmount = totalPrice,
            Status = order.Status,
            DeliveryAddresses = addresses,
            PaymentMethods = paymentMethods
        };
    }

    public Task<OrderDto> UpdateOrderAsync(Guid userId)
    {
        return _transactionManager.ExecuteAsync(() =>
        {
            return UpdateOrderCoreAsync(userId);
        });
    }

    private async Task<OrderDto> UpdateOrderCoreAsync(Guid userId)
    {
        var user = await LockUserForOrderChangeAsync(userId);

        var addresses = await _userRepository.GetUserDeliveryAddressesAsync(user);
        if (addresses == null)
        {
            throw new ApiException(ErrorCodes.UserNotFound, "User not found", StatusCodes.Status404NotFound);
        }
        
        var paymentMethods = await _userRepository.GetUserPaymentMethodsAsync(user);
        if (paymentMethods == null)
        {
            throw new ApiException(ErrorCodes.UserNotFound, "User not found", StatusCodes.Status404NotFound);
        }
        
        var cart = await ValidateCartAsync(userId);

        var order = await _orderRepository.GetPendingOrderByUserIdAsync(user.Id);
        if (order == null)
        {
            throw new ApiException(ErrorCodes.OrderNotFound, "Order not found", StatusCodes.Status404NotFound);
        }
        
        int quantity = cart.Items.Sum(item =>
        {
            return item.Quantity;
        });
        decimal totalPrice = cart.Items.Sum(item =>
        {
            return item.Product.CurrentPrice * item.Quantity;
        });

        if (quantity == 0)
        {
            throw new ApiException(ErrorCodes.CartNotFound, "Cart is empty", StatusCodes.Status404NotFound);
        }

        _orderRepository.ReplaceOrderItems(order, BuildOrderItems(cart, order));
        order.TotalAmount = totalPrice;
        order.UpdatedAt = DateTime.Now;
        await _orderRepository.SaveChangesAsync();
        
        return new OrderDto()
        {
            OrderId = order.Id,
            TotalQuantity = quantity,
            TotalAmount = totalPrice,
            Status = order.Status,
            DeliveryAddresses = addresses,
            PaymentMethods = paymentMethods
        };
    }

    public Task<CompletedOrderDto> CompleteOrderAsync(Guid userId, Guid paymentMethodId, Guid deliveryAddressId)
    {
        return _transactionManager.ExecuteAsync(() =>
        {
            return CompleteOrderCoreAsync(userId, paymentMethodId, deliveryAddressId);
        });
    }

    private async Task<CompletedOrderDto> CompleteOrderCoreAsync(Guid userId,
        Guid paymentMethodId, 
        Guid deliveryAddressId)
    {
        await LockUserForOrderChangeAsync(userId);
        
        var address = await _userRepository.GetDeliveryAddressByIdAsync(userId, deliveryAddressId);
        if (address == null)
        {
            throw new ApiException(ErrorCodes.UserNotFound, "Delivery address not found", StatusCodes.Status404NotFound);
        }
        
        var paymentMethod = await _userRepository.GetPaymentMethodByIdAsync(userId, paymentMethodId);
        if (paymentMethod == null)
        {
            throw new ApiException(ErrorCodes.UserNotFound, "Payment method not found", StatusCodes.Status404NotFound);
        }
        
        if (paymentMethod.ExpirationDate < DateOnly.FromDateTime(DateTime.Now))
        {
            throw new ApiException(ErrorCodes.ValidationError, "Payment method is expired",
                StatusCodes.Status406NotAcceptable);
        }
        
        var cart = await ValidateCartAsync(userId, includeItems: false);
        var order = await _orderRepository.GetPendingOrderByUserIdAsync(userId);
        if (order == null)
        {
            throw new ApiException(ErrorCodes.OrderNotFound, "Order not found", StatusCodes.Status404NotFound);
        }

        var productIds = order.OrderItems.Select(item =>
        {
            return item.ProductId;
        }).Distinct().ToArray();
        // Читаем остатки под блокировкой, чтобы параллельный заказ не списал те же единицы.
        // Поэтому выше загрузили корзину без товаров: в контексте не должно быть старых остатков.
        var products = await _productRepository.GetProductsForUpdateAsync(productIds);
        var productsById = products.ToDictionary(product =>
        {
            return product.Id;
        });
        foreach (var item in order.OrderItems)
        {
            if (!productsById.TryGetValue(item.ProductId, out var product))
            {
                throw new ApiException(ErrorCodes.ProductNotFound, "Product not found", StatusCodes.Status404NotFound);
            }

            if (product.StockQuantity < item.Quantity)
            {
                throw new ApiException(ErrorCodes.ProductOutOfStock, "Product is out of stock", StatusCodes.Status409Conflict);
            }
            
            product.StockQuantity -= item.Quantity;
        }

        await _cartRepository.ClearCartAsync(cart.Id);
        
        order.Status = OrderStatus.Confirmed;
        order.ConfirmedAt = DateTime.Now;
        order.DeliveryAddress = address;
        order.PaymentMethod = paymentMethod;
        await _orderRepository.SaveChangesAsync();
        return new CompletedOrderDto()
        {
            OrderId = order.Id,
            TotalQuantity = order.OrderItems.Sum(oi => oi.Quantity),
            TotalAmount = order.TotalAmount,
            Status = order.Status,
            DeliveryAddress = address,
            PaymentMethod = paymentMethod
        };
    }

    public Task<CompletedOrderDto> CancelOrderAsync(Guid userId)
    {
        return _transactionManager.ExecuteAsync(() =>
        {
            return CancelOrderCoreAsync(userId);
        });
    }

    private async Task<CompletedOrderDto> CancelOrderCoreAsync(Guid userId)
    {
        await LockUserForOrderChangeAsync(userId);
        
        var cart = await ValidateCartAsync(userId, includeItems: false);
        var order = await _orderRepository.GetPendingOrderByUserIdAsync(userId);
        if (order == null)
        {
            throw new ApiException(ErrorCodes.OrderNotFound, "Order not found", StatusCodes.Status404NotFound);
        }

        await _cartRepository.ClearCartAsync(cart.Id);
        order.Status = OrderStatus.Cancelled;
        order.CanceledAt = DateTime.Now;
        await _orderRepository.SaveChangesAsync();

        return new CompletedOrderDto()
        {
            OrderId = order.Id,
            TotalQuantity = order.OrderItems.Sum(oi => oi.Quantity),
            TotalAmount = order.TotalAmount,
            Status = order.Status
        };
    }

    public async Task<List<CompletedOrderDto>> GetOrdersAsync(Guid userId)
    {
        var user = await _userRepository.GetUserByIdAsync(userId);
        if (user == null)
        {
            throw new ApiException(ErrorCodes.UserNotFound, "User not found", StatusCodes.Status404NotFound);
        }
        
        var orders = await _orderRepository.GetOrdersAsync(user.Id);
        if (orders == null)
        {
            throw new ApiException(ErrorCodes.OrderNotFound, "Orders not found", StatusCodes.Status404NotFound);
        }
        
        List<CompletedOrderDto> completedOrders = new List<CompletedOrderDto>();
        foreach (var order in orders)
        {
            completedOrders.Add(new CompletedOrderDto()
            {
                OrderId = order.Id,
                TotalQuantity = order.OrderItems.Sum(oi => oi.Quantity),
                TotalAmount = order.TotalAmount,
                Status = order.Status,
                CreatedAt = order.CreatedAt,
                CompletedAt = order.ConfirmedAt,
                CanceledAt = order.CanceledAt,
                DeliveryAddress = order.DeliveryAddress,
                PaymentMethod = order.PaymentMethod
            });
        }
        return completedOrders;   
    }
    
    private async Task<User> LockUserForOrderChangeAsync(Guid userId)
    {
        // Пока открыта транзакция, другой запрос этого пользователя не изменит тот же черновик.
        return await _userRepository.GetByIdForUpdateAsync(userId)
            ?? throw new ApiException(ErrorCodes.UserNotFound, "User not found", StatusCodes.Status404NotFound);
    }

    private static List<OrderItem> BuildOrderItems(Cart cart, Order order)
    {
        return cart.Items.Select(item =>
        {
            return new OrderItem
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                ProductId = item.ProductId,
                Brand = item.Product.Brand.Name,
                ProductTitle = item.Product.Title,
                ProductImage = item.Product.PrimaryImageUrl,
                Quantity = item.Quantity,
                UnitPrice = item.Product.CurrentPrice,
                Order = order
            };
        }).ToList();
    }

    private async Task<Cart> ValidateCartAsync(Guid userId, bool includeItems = true)
    {
        var cart = await _cartRepository.GetCartByUserIdAsync(userId, includeItems);
        if (cart == null)
        {
            throw new ApiException(ErrorCodes.CartNotFound, "Cart not found", StatusCodes.Status404NotFound);
        }

        bool isEmpty = includeItems
            ? cart.Items.Count == 0
            : await _cartRepository.IsCartEmptyAsync(cart.Id);
        if (isEmpty)
        {
            throw new ApiException(ErrorCodes.ProductNotFound, "Cart is empty", StatusCodes.Status404NotFound);
        }
        
        return cart;
    }
    
}
