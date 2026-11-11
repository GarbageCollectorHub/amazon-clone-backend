using AMZN.Data.Entities;

namespace AMZN.Repositories.Users
{
    public interface IUserRepository
    {
        Task AddUserAsync(User user);
        Task<bool> IsEmailTakenAsync(string email);
        Task<User?> GetByEmailAsync(string email);
        
        Task<User?> GetUserByIdAsync(Guid id);
        Task<User?> GetByIdForUpdateAsync(Guid id);
        
        Task UpdateUserAsync(User user);
        
        Task DeleteUserAsync(User user);
        
        Task AddPaymentMethodAsync(PaymentMethod paymentMethod, User user);
        
        Task<List<PaymentMethod>> GetUserPaymentMethodsAsync(User user);
        
        Task DeletePaymentMethodAsync(PaymentMethod paymentMethod, User user);
        
        void SetDefaultPaymentMethod(PaymentMethod paymentMethod, User user);
        
        Task<PaymentMethod?> GetDefaultPaymentMethodAsync(User user);
        
        Task AddDeliveryAddressAsync(DeliveryAddress address, User user);
        
        Task<List<DeliveryAddress>> GetUserDeliveryAddressesAsync(User user);
        
        Task DeleteDeliveryAddressAsync(DeliveryAddress address, User user);
        
        void SetDefaultDeliveryAddress(DeliveryAddress address, User user);
        
        Task<DeliveryAddress?> GetDeliveryAddressByIdAsync(Guid userId, Guid id);
        
        Task<PaymentMethod?> GetPaymentMethodByIdAsync(Guid userId, Guid id);
    }
}
