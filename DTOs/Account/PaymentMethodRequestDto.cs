using System.ComponentModel.DataAnnotations;
using AMZN.Shared.Validation;

namespace AMZN.DTOs.Account;

public class PaymentMethodRequestDto
{
    private const string CardRegex = @"^\d{13,19}?$";
    private const string CvvRegex = @"^\d{3,4}$";
    
    [Display(Name = "Card Number")]
    [Required]
    [RegularExpression(CardRegex, ErrorMessage = ValidationMessages.InvalidFormat)]
    public string CardNumber { get; set; } = "";
    
    [Display(Name = "CVV")]
    [Required]
    [RegularExpression(CvvRegex, ErrorMessage = ValidationMessages.InvalidFormat)]
    public string Cvv { get; set; } = "";
    
    [Display(Name = "Expiration Date")]
    [Required]
    public DateOnly ExpirationDate { get; set; }
    
    [Display(Name = "Set as default payment method")]
    public bool IsDefault { get; set; }
}
