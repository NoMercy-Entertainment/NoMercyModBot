using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ModBot.Database.Models;

namespace ModBot.Database;

public class TokenStore
{
    private static readonly IConfiguration Configuration = new ConfigurationBuilder()
        .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
        .AddJsonFile("appsettings.json", false, true)
        .Build();

    private static string SecretToken { get; } = Configuration["SECRET_TOKEN"] ??
                                                 throw new InvalidOperationException("SECRET_TOKEN not found.");

    private static readonly IDataProtectionProvider Provider = DataProtectionProvider.Create("ModBot.Server");
    private static readonly IDataProtector Protector = Provider.CreateProtector(SecretToken);
    private static readonly AppDbContext DbContext = new();

    public static void SaveToken(string userId, string accessToken, string refreshToken)
    {
        User? user = DbContext.Users.FirstOrDefault(u => u.Id == userId);
        if (user is null) throw new InvalidOperationException("Moderator not found.");

        string encryptedAccessToken = Protector.Protect(accessToken);
        string encryptedRefreshToken = Protector.Protect(refreshToken);

        DbContext.Users.Upsert(user)
            .On(u => u.Id)
            .WhenMatched(u => new()
            {
                AccessToken = encryptedAccessToken,
                RefreshToken = encryptedRefreshToken
            })
            .Run();
    }

    public static (string accessToken, string refreshToken) GetTokens(string userId)
    {
        User? user = DbContext.Users.FirstOrDefault(u => u.Id == userId);
        if (user is null) throw new InvalidOperationException("Moderator not found.");

        string accessToken = Protector.Unprotect(user.AccessToken);
        string refreshToken = Protector.Unprotect(user.RefreshToken);

        return (accessToken, refreshToken);
    }

    public static (string accessToken, string refreshToken) EncryptToken(string accessToken, string refreshToken)
    {
        string encryptedAccessToken = Protector.Protect(accessToken);
        string encryptedRefreshToken = Protector.Protect(refreshToken);

        return (encryptedAccessToken, encryptedRefreshToken);
    }

    public static (string accessToken, string refreshToken) DecryptToken(string accessToken, string refreshToken)
    {
        string decryptedAccessToken = Protector.Unprotect(accessToken);
        string decryptedRefreshToken = Protector.Unprotect(refreshToken);

        return (decryptedAccessToken, decryptedRefreshToken);
    }

    public static string GetAccessToken(User user)
    {
        return Protector.Unprotect(user.AccessToken);
    }

    public static string GetRefreshToken(User user)
    {
        return Protector.Unprotect(user.RefreshToken);
    }

    public static string? DecryptToken(string? accessToken)
    {
        return Protector.Unprotect(accessToken);
    }

    public static string EncryptToken(string? token)
    {
        return Protector.Protect(token);
    }
}