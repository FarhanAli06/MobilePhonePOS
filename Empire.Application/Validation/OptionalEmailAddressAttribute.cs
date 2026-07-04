using System.ComponentModel.DataAnnotations;

namespace Empire.Application.Validation;

/// <summary>
/// Validates that a string is a valid email address format.
/// Unlike [EmailAddress], this attribute allows null or empty values.
/// </summary>
public class OptionalEmailAddressAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        // Allow null or empty values
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
        {
            return ValidationResult.Success;
        }

        // If a value is provided, validate it as an email address
        var emailAttribute = new EmailAddressAttribute();
        if (!emailAttribute.IsValid(value))
        {
            return new ValidationResult(ErrorMessage ?? "The Email field is not a valid e-mail address.");
        }

        return ValidationResult.Success;
    }
}
