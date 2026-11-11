using System.ComponentModel.DataAnnotations;
using AMZN.Shared.Limits;
using AMZN.Shared.Validation;
namespace AMZN.DTOs.Auth;

public class ProfileUpdateRequestDto
{
    
    [Display(Name = "First name")]
    [Required]
    [StringLength(UserLimits.PersonNameMaxLength, MinimumLength = UserLimits.PersonNameMinLength, ErrorMessage = ValidationMessages.LengthRange)]
    [RegularExpression(UserLimits.PersonNamePattern, ErrorMessage = ValidationMessages.PersonNameFormat)]
    public string FirstName { get; set; } = "";
    
    [Display(Name = "Last name")]
    [Required]
    [StringLength(UserLimits.PersonNameMaxLength, MinimumLength = UserLimits.PersonNameMinLength, ErrorMessage = ValidationMessages.LengthRange)]
    [RegularExpression(UserLimits.PersonNamePattern, ErrorMessage = ValidationMessages.PersonNameFormat)]
    public string LastName { get; set; } = "";
    
    [Display(Name = "Email")]
    [Required]
    [EmailAddress(ErrorMessage = ValidationMessages.InvalidFormat)]
    [StringLength(UserLimits.EmailMaxLength, ErrorMessage = ValidationMessages.MaxLength)]
    public string Email { get; set; } = "";
    
    [Display(Name = "Phone number")]
    [StringLength(UserLimits.ProfilePhoneMaxLength, ErrorMessage = ValidationMessages.MaxLength)]
    [RegularExpression(UserLimits.PhoneNumberPattern, ErrorMessage = ValidationMessages.InvalidFormat)]
    public string PhoneNumber { get; set; } = "";
    
    [Display(Name = "Date of Birth")]
    [DataType(DataType.Date)]
    public DateOnly? BirthDate { get; set; }
    
}
