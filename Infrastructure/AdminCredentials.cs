using Microsoft.AspNetCore.Identity;

namespace DevFolio.Infrastructure;

/// <summary>
/// Single admin account configured through settings / environment variables:
/// Admin__Username and Admin__PasswordHash (generate the hash with `dotnet DevFolio.dll hash-password &lt;password&gt;`).
/// If either value is missing, admin sign-in is disabled (fail closed).
/// </summary>
public class AdminCredentials
{
    public const string SectionName = "Admin";

    private static readonly PasswordHasher<string> Hasher = new();

    public string? Username { get; set; }

    public string? PasswordHash { get; set; }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(PasswordHash);

    public bool Verify(string? username, string? password)
    {
        if (!IsConfigured || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            return false;
        }

        // Always run the hash check so a wrong username takes the same time as a wrong password.
        var result = Hasher.VerifyHashedPassword(Username!, PasswordHash!, password);
        var usernameMatches = string.Equals(username, Username, StringComparison.OrdinalIgnoreCase);

        return usernameMatches && result != PasswordVerificationResult.Failed;
    }

    public static string HashPassword(string password) => Hasher.HashPassword("admin", password);
}
