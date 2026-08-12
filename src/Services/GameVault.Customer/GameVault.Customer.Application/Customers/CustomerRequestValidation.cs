using System.Text.RegularExpressions;
using GameVault.Customer.Application.Errors;
using GameVault.SharedKernel.Results;

namespace GameVault.Customer.Application.Customers;

internal static partial class CustomerRequestValidation
{
    // Mirrors the column constraints in CustomerConfiguration (Infrastructure) — Application
    // cannot reference Infrastructure, so these limits are deliberately duplicated here.
    private const int EmailMaxLength = 256;
    private const int NameMaxLength = 100;
    private const int PhoneNumberMaxLength = 30;
    private const int PasswordMinLength = 8;

    public static Error? ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return CustomerErrors.Required(nameof(email));

        if (email.Length > EmailMaxLength)
            return CustomerErrors.TooLong(nameof(email), EmailMaxLength);

        if (!EmailRegex().IsMatch(email))
            return CustomerErrors.InvalidEmail;

        return null;
    }

    public static Error? ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return CustomerErrors.Required(nameof(password));

        if (password.Length < PasswordMinLength)
            return CustomerErrors.WeakPassword(PasswordMinLength);

        return null;
    }

    public static Error? ValidateName(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
            return CustomerErrors.Required(fieldName);

        if (value.Length > NameMaxLength)
            return CustomerErrors.TooLong(fieldName, NameMaxLength);

        return null;
    }

    public static Error? ValidatePhoneNumber(string? phoneNumber)
    {
        if (phoneNumber is null)
            return null;

        if (phoneNumber.Length > PhoneNumberMaxLength)
            return CustomerErrors.TooLong(nameof(phoneNumber), PhoneNumberMaxLength);

        if (!PhoneNumberRegex().IsMatch(phoneNumber))
            return CustomerErrors.InvalidPhoneNumber;

        return null;
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();

    // Accepts optional leading +, then at least 7 chars from digits, spaces, hyphens, parentheses, and dots.
    [GeneratedRegex(@"^\+?[\d ()\-.]{7,}$")]
    private static partial Regex PhoneNumberRegex();
}
