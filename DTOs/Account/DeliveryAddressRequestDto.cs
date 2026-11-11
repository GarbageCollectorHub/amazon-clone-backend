using System.ComponentModel.DataAnnotations;
using AMZN.Shared.Limits;
using AMZN.Shared.Validation;

namespace AMZN.DTOs.Account;

public class DeliveryAddressRequestDto
{
    private const int PostalCodeMinLength = 2;
    private const int PostalCodeMaxLength = 10;
    private const int CityMinLength = 2;
    private const int CityMaxLength = 32;
    private const string AddressRegex = @"^[\w\s\d.,\-\/]{5,100}$";
    private const string CityRegex = @"^[\p{L}][\p{L}\s.'-]*[\p{L}]$";
    private const string CountryRegex = @"^[\p{L}][\p{L}\s.'()-]{1,60}[\p{L}.]$";
    private const string StateRegex = @"^[\p{L}0-9\s.'-]{2,32}$";
    
    [Display(Name = "First Name")]
    [Required]
    [StringLength(UserLimits.PersonNameMaxLength, MinimumLength = UserLimits.PersonNameMinLength, ErrorMessage = ValidationMessages.LengthRange)]
    [RegularExpression(UserLimits.PersonNamePattern, ErrorMessage = ValidationMessages.PersonNameFormat)]
    public string FirstName { get; set; } = "";
    
    [Display(Name = "Last Name")]
    [Required]
    [StringLength(UserLimits.PersonNameMaxLength, MinimumLength = UserLimits.PersonNameMinLength, ErrorMessage = ValidationMessages.LengthRange)]
    [RegularExpression(UserLimits.PersonNamePattern, ErrorMessage = ValidationMessages.PersonNameFormat)]
    public string LastName { get; set; } = "";
    
    [Display(Name = "Street Address")]
    [Required]
    [RegularExpression(AddressRegex, ErrorMessage = "{0} invalid format")]
    public string StreetAddress { get; set; } = "";
    
    [Display(Name = "City")]
    [Required]
    [StringLength(CityMaxLength, MinimumLength = CityMinLength, ErrorMessage = ValidationMessages.LengthRange)]
    [RegularExpression(CityRegex, ErrorMessage = "{0} invalid format")]
    public string City { get; set; } = "";

    [Display(Name = "Postal Code")]
    [Required]
    [StringLength(PostalCodeMaxLength, MinimumLength = PostalCodeMinLength, ErrorMessage = ValidationMessages.LengthRange)]
    public string PostalCode { get; set; } = "";
    
    [Display(Name = "Country")]
    [Required]
    [RegularExpression(CountryRegex, ErrorMessage = "{0} invalid format")]
    public string Country { get; set; } = "";

    [Display(Name = "State")]
    [RegularExpression(StateRegex, ErrorMessage = "{0} invalid format")]
    public string? State { get; set; } = "";
    
    [Display(Name = "Phone number")]
    [Required]
    [RegularExpression(UserLimits.PhoneNumberPattern, ErrorMessage = ValidationMessages.InvalidFormat)]
    public string PhoneNumber { get; set; } = "";
    
    [Display(Name = "Set as default delivery address")]
    public bool IsDefault { get; set; }
    
    
    
}
