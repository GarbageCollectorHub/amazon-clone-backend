using System.ComponentModel.DataAnnotations;

namespace AMZN.DTOs.Auth
{
    public class RegisterRequestDto
    {
        private const string PersonNameRegex = @"^[\p{L}\p{M}]+(?:[ '\-][\p{L}\p{M}]+)*$";      // Буквы(любой алфавит), между частями допустимы пробел/дефис/апостроф
        private const int NameMinLength = 3;
        private const int NameMaxLength = 64;
        private const int EmailMaxLength = 128;
        private const int PasswordMinLength = 6;
        private const int PasswordMaxLength = 72;
        

        [Display(Name = "First name")]
        [Required(ErrorMessage = "{0} is required")]
        [StringLength(NameMaxLength, MinimumLength = NameMinLength, ErrorMessage = "{0} must be {2}-{1} characters")]
        [RegularExpression(PersonNameRegex, ErrorMessage = "{0} may contain letters, spaces, hyphens and apostrophes")]
        public string FirstName { get; set; } = "";


        [Display(Name = "Last name")]
        [Required(ErrorMessage = "{0} is required")]
        [StringLength(NameMaxLength, MinimumLength = NameMinLength, ErrorMessage = "{0} must be {2}-{1} characters")]
        [RegularExpression(PersonNameRegex, ErrorMessage = "{0} may contain letters, spaces, hyphens and apostrophes")]
        public string LastName { get; set; } = "";


        [Display(Name = "Email")]
        [Required(ErrorMessage = "{0} is required")]
        [EmailAddress(ErrorMessage = "Invalid {0} format")]
        [StringLength(EmailMaxLength, ErrorMessage = "{0} must be {1} characters or less")]
        public string Email { get; set; } = "";


        [Display(Name = "Password")]
        [Required(ErrorMessage = "{0} is required")]
        [StringLength(PasswordMaxLength, MinimumLength = PasswordMinLength, ErrorMessage = "{0} must be {2}-{1} characters")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$", ErrorMessage = "{0} must include uppercase, lowercase, and a number")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";


        [Display(Name = "Password Repeat")]
        [Required(ErrorMessage = "{0} is required")]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        [DataType(DataType.Password)]
        public string PasswordRepeat { get; set; } = "";



    }
}
