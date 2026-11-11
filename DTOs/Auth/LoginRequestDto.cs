using System.ComponentModel.DataAnnotations;
using AMZN.Shared.Limits;
using AMZN.Shared.Validation;

namespace AMZN.DTOs.Auth
{
    public class LoginRequestDto
    {


        [Display(Name = "Email")]
        [Required(ErrorMessage = ValidationMessages.Required)]
        [EmailAddress(ErrorMessage = ValidationMessages.InvalidFormat)]
        [StringLength(UserLimits.LoginEmailMaxLength, ErrorMessage = ValidationMessages.MaxLength)]
        public string Email { get; set; } = "";


        [Display(Name = "Password")]
        [Required(ErrorMessage = ValidationMessages.Required)]
        [StringLength(UserLimits.PasswordMaxLength, ErrorMessage = ValidationMessages.MaxLength)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";

    }
}
