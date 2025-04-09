using System.Security.Authentication;
using System.Security.Claims;
using ModBot.Database;
using ModBot.Database.Models;

namespace ModBot.Server.Helpers;

public static class ClaimsPrincipleExtensions
{
    public static int UserId(this ClaimsPrincipal? principal)
    {
        string? userId = principal?
            .FindFirst(ClaimTypes.NameIdentifier)?
            .Value;

        return int.TryParse(userId, out int parsedUserId)
            ? parsedUserId
            : throw new AuthenticationException("Moderator ID not found");
    }

    public static string Role(this ClaimsPrincipal? principal)
    {
        return principal?
                   .FindFirst(ClaimTypes.Role)?
                   .Value
               ?? throw new AuthenticationException("Role not found");
    }

    public static string UserName(this ClaimsPrincipal? principal)
    {
        try
        {
            return principal?.FindFirst("name")?.Value
                   ?? principal?.FindFirst(ClaimTypes.GivenName)?.Value + " " +
                   principal?.FindFirst(ClaimTypes.Surname)?.Value;
        }
        catch (Exception e)
        {
            throw new AuthenticationException("Moderator name not found");
        }
    }

    public static string Email(this ClaimsPrincipal? principal)
    {
        try
        {
            return principal?.FindFirst(ClaimTypes.Email)?.Value
                   ?? throw new AuthenticationException("Email not found");
        }
        catch (Exception e)
        {
            throw new AuthenticationException("Moderator name not found");
        }
    }

    public static User? User(this ClaimsPrincipal? principal)
    {
        string? userId = principal?
            .FindFirst(ClaimTypes.NameIdentifier)?
            .Value;
        
        using AppDbContext dbContext = new();

        return userId is null
            ? null
            : dbContext.Users
                .FirstOrDefault(user => user.Id == userId);
    }
}