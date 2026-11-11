using System.ComponentModel.DataAnnotations;
using AMZN.Shared.Limits;
using AMZN.Shared.Validation;

namespace AMZN.DTOs.Auth
{
    public class RefreshRequestDto
    {

        [Display(Name = "Refresh token")]
        [Required(ErrorMessage = ValidationMessages.Required, AllowEmptyStrings = false)]
        [StringLength(JwtLimits.RefreshTokenRequestMaxLength, ErrorMessage = ValidationMessages.MaxLength)]
        public string RefreshToken { get; set; } = "";
    }
}
