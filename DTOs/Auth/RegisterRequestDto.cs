using System.ComponentModel.DataAnnotations;
using AMZN.Shared.Limits;
using AMZN.Shared.Validation;

namespace AMZN.DTOs.Auth
{
    public class RegisterRequestDto
    {
        

        [Display(Name = "First name")]
        [Required(ErrorMessage = ValidationMessages.Required)]
        [StringLength(UserLimits.PersonNameMaxLength, MinimumLength = UserLimits.PersonNameMinLength, ErrorMessage = ValidationMessages.LengthRange)]
        [RegularExpression(UserLimits.PersonNamePattern, ErrorMessage = ValidationMessages.PersonNameFormat)]
        public string FirstName { get; set; } = "";


        [Display(Name = "Last name")]
        [Required(ErrorMessage = ValidationMessages.Required)]
        [StringLength(UserLimits.PersonNameMaxLength, MinimumLength = UserLimits.PersonNameMinLength, ErrorMessage = ValidationMessages.LengthRange)]
        [RegularExpression(UserLimits.PersonNamePattern, ErrorMessage = ValidationMessages.PersonNameFormat)]
        public string LastName { get; set; } = "";


        [Display(Name = "Email")]
        [Required(ErrorMessage = ValidationMessages.Required)]
        [EmailAddress(ErrorMessage = ValidationMessages.InvalidFormat)]
        [StringLength(UserLimits.EmailMaxLength, ErrorMessage = ValidationMessages.MaxLength)]
        public string Email { get; set; } = "";


        [Display(Name = "Password")]
        [Required(ErrorMessage = ValidationMessages.Required)]
        [StringLength(UserLimits.PasswordMaxLength, MinimumLength = UserLimits.PasswordMinLength, ErrorMessage = ValidationMessages.LengthRange)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$", ErrorMessage = "{0} must include uppercase, lowercase, and a number")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";


        [Display(Name = "Password Repeat")]
        [Required(ErrorMessage = ValidationMessages.Required)]
        [Compare(nameof(Password), ErrorMessage = "Passwords do not match")]
        [DataType(DataType.Password)]
        public string PasswordRepeat { get; set; } = "";



    }
}
