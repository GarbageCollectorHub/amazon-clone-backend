using AMZN.Data;
using AMZN.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AMZN.Repositories.Orders;

public class OrderRepository : IOrderRepository
{
    private readonly DataContext _dataContext;
    
    public OrderRepository(DataContext dataContext)
    {
        _dataContext = dataContext;
    }
    
    public async Task AddOrderAsync(Order order)
    {
        await _dataContext.Orders.AddAsync(order);
        await _dataContext.SaveChangesAsync();
    }

    public async Task<Order?> GetPendingOrderByUserIdAsync(Guid userId)
    {
        return await _dataContext.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.UserId == userId && o.Status == OrderStatus.Pending);
    }
    
    public Task SaveChangesAsync()
    {
        return _dataContext.SaveChangesAsync();
    }

    public void ReplaceOrderItems(Order order, List<OrderItem> items)
    {
        _dataContext.OrderItems.RemoveRange(order.OrderItems);
        order.OrderItems.Clear();
        _dataContext.OrderItems.AddRange(items);
    }
    
    public async Task<List<Order>?> GetOrdersAsync(Guid userId)
    {
        return await _dataContext.Orders
            .Include(o => o.OrderItems)
            .Where(o => o.UserId == userId && o.Status != OrderStatus.Pending)
            .ToListAsync();
    }
}
