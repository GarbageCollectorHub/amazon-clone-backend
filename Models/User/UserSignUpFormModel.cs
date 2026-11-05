using System.ComponentModel.DataAnnotations;

namespace AMZN.Models.User;

public class UserSignUpFormModel
{
    [Required(ErrorMessage = "First name is required")]
    [StringLength(20, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 20 characters")]
    public string FirstName { get; set; } = null!;
    
    [Required(ErrorMessage = "Last name is required")]
    [StringLength(20, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 20 characters")]
    public string LastName { get; set; } = null!;
    
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Email format is invalid")]
    [StringLength(128, ErrorMessage = "Email must be 128 characters or less")]
    public string Email { get; set; } = null!;
    
    [Required(ErrorMessage = "Password is required")]
    [StringLength(20, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 20 characters")]
    public string Password { get; set; } = null!;
    
    [Required(ErrorMessage = "Password confirmation is required")]
    [StringLength(20, MinimumLength = 8, ErrorMessage = "Password confirmation must be between 8 and 20 characters")]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match")]
    public string ConfirmPassword { get; set; } = null!;

    [Required(ErrorMessage = "Role is required")]
    [RegularExpression("^(User|Admin|Moderator)$", ErrorMessage = "Role is not valid")]
    public string Role { get; set; } = null!;
}
