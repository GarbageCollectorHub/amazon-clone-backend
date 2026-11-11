using System.ComponentModel.DataAnnotations;
using AMZN.Shared.Limits;

namespace AMZN.Models.User;

public class UserSignUpFormModel
{
    [Required(ErrorMessage = "First name is required")]
    [StringLength(UserLimits.AdminSignUpNameMaxLength, MinimumLength = UserLimits.AdminSignUpNameMinLength, ErrorMessage = "First name must be between {2} and {1} characters")]
    public string FirstName { get; set; } = null!;
    
    [Required(ErrorMessage = "Last name is required")]
    [StringLength(UserLimits.AdminSignUpNameMaxLength, MinimumLength = UserLimits.AdminSignUpNameMinLength, ErrorMessage = "Last name must be between {2} and {1} characters")]
    public string LastName { get; set; } = null!;
    
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Email format is invalid")]
    [StringLength(UserLimits.EmailMaxLength, ErrorMessage = "Email must be {1} characters or less")]
    public string Email { get; set; } = null!;
    
    [Required(ErrorMessage = "Password is required")]
    [StringLength(UserLimits.AdminSignUpPasswordMaxLength, MinimumLength = UserLimits.AdminSignUpPasswordMinLength, ErrorMessage = "Password must be between {2} and {1} characters")]
    public string Password { get; set; } = null!;
    
    [Required(ErrorMessage = "Password confirmation is required")]
    [StringLength(UserLimits.AdminSignUpPasswordMaxLength, MinimumLength = UserLimits.AdminSignUpPasswordMinLength, ErrorMessage = "Password confirmation must be between {2} and {1} characters")]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match")]
    public string ConfirmPassword { get; set; } = null!;

    [Required(ErrorMessage = "Role is required")]
    [RegularExpression("^(User|Admin|Moderator)$", ErrorMessage = "Role is not valid")]
    public string Role { get; set; } = null!;
}
