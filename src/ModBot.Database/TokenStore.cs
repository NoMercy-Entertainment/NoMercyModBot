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

    public static string? DecryptToken(string? accessToken)
    {
        return Protector.Unprotect(accessToken);
    }

    public static string EncryptToken(string? token)
    {
        return Protector.Protect(token);
    }
}