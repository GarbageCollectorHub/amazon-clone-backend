using AMZN.Data;
using AMZN.Data.Entities;
using AMZN.Models.User;
using AMZN.Repositories.Users;
using AMZN.Security.Passwords;

namespace AMZN.Services.Admin;

public class AdminUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    
    public AdminUserService(IUserRepository userRepository,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<Dictionary<string, string>> RegisterUser(UserSignUpFormModel formModel)
    {
        string email = formModel.Email.Trim().ToLowerInvariant();
        if (await _userRepository.IsEmailTakenAsync(email))
            return new() { [nameof(formModel.Email)] = "Email already taken" };

        User user = new()
        {
            Id = Guid.NewGuid(),
            FirstName = formModel.FirstName.Trim(),
            LastName = formModel.LastName.Trim(),
            Email = email,
            PasswordHash = _passwordHasher.HashPassword(formModel.Password),
            Role = Enum.Parse<UserRole>(formModel.Role),
            CreatedAt = DateTime.UtcNow,
        };

        await _userRepository.AddUserAsync(user);
        return new();
    }
}

