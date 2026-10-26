namespace BrokerPortal.Infrastructure.Services;

public sealed class ActivationPasswordOptions
{
    public int MinimumLength { get; set; } = 12;
    public bool RequireUppercase { get; set; } = true;
    public bool RequireLowercase { get; set; } = true;
    public bool RequireDigit { get; set; } = true;
    public bool RequireNonAlphanumeric { get; set; } = true;

    public string? GetValidationMessage(string password)
    {
        if (password.Length < MinimumLength ||
            (RequireUppercase && !password.Any(char.IsUpper)) ||
            (RequireLowercase && !password.Any(char.IsLower)) ||
            (RequireDigit && !password.Any(char.IsDigit)) ||
            (RequireNonAlphanumeric && !password.Any(character => !char.IsLetterOrDigit(character))))
        {
            return $"Use at least {MinimumLength} characters" +
                $"{(RequireUppercase ? ", including an uppercase letter" : string.Empty)}" +
                $"{(RequireLowercase ? ", a lowercase letter" : string.Empty)}" +
                $"{(RequireDigit ? ", a number" : string.Empty)}" +
                $"{(RequireNonAlphanumeric ? ", and a symbol" : string.Empty)}.";
        }

        return null;
    }
}
