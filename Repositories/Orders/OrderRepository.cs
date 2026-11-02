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

    public async Task AddOrderItemAsync(OrderItem orderItem)
    {
        await _dataContext.OrderItems.AddAsync(orderItem);
        var order = _dataContext.Orders.FirstOrDefault(o => o.Id == orderItem.OrderId);
        if (order == null) return;
        order.OrderItems.Add(orderItem);
        await _dataContext.SaveChangesAsync();   
    }

    public async Task<Order?> GetPendingOrderByUserIdAsync(Guid userId)
    {
        return await _dataContext.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.UserId == userId && o.Status == OrderStatus.Pending);
    }
    
    public async Task UpdateOrderAsync(Order order)
    {
        _dataContext.Orders.Update(order);
        await _dataContext.SaveChangesAsync();
    }

    public async Task RemoveOrderItemsAsync(Order order)
    {
        _dataContext.OrderItems.RemoveRange(order.OrderItems);
        order.OrderItems.Clear();
        await _dataContext.SaveChangesAsync();
    }
    
    public async Task<List<Order>?> GetOrdersAsync(Guid userId)
    {
        return await _dataContext.Orders
            .Include(o => o.OrderItems)
            .Where(o => o.UserId == userId && o.Status != OrderStatus.Pending)
            .ToListAsync();
    }
}
