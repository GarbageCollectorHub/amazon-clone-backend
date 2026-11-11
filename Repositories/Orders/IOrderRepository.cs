using AMZN.Data.Entities;

namespace AMZN.Repositories.Orders;

public interface IOrderRepository
{
    Task AddOrderAsync(Order order);
    Task<Order?> GetPendingOrderByUserIdAsync(Guid userId);
    Task SaveChangesAsync();
    void ReplaceOrderItems(Order order, List<OrderItem> items);
    Task<List<Order>?> GetOrdersAsync(Guid userId);
}
