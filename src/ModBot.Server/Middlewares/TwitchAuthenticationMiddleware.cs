using Microsoft.Extensions.Caching.Memory;
using ModBot.Server.Config;
using ModBot.Server.Helpers;
using Newtonsoft.Json;

namespace ModBot.Server.Middlewares;

public class TwitchAuthenticationMiddleware(RequestDelegate next, IMemoryCache cache)
{
    public async Task InvokeAsync(HttpContext context)
    {
        string[] excludedPaths =
        [
            "/api/auth",
            "/api/auth/acquire-token",
            "/api/auth/callback"
        ];

        if (excludedPaths.Contains(context.Request.Path.Value, StringComparer.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        string? authorizationHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith("Bearer "))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Missing or invalid Authorization header.");
            return;
        }

        string token = authorizationHeader["Bearer ".Length..];

        if (!cache.TryGetValue(token, out ValidationResponse? tokenInfo))
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("Authorization", $"OAuth {token}");
            HttpResponseMessage response = await client.GetAsync($"{Globals.TwitchAuthUrl}/validate");

            if (!response.IsSuccessStatusCode)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Invalid token.");
                return;
            }

            string validationResponse = await response.Content.ReadAsStringAsync();
            tokenInfo = validationResponse.FromJson<ValidationResponse>();
            if (tokenInfo is null)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Invalid token.");
                return;
            }

            cache.Set(token, tokenInfo, TimeSpan.FromSeconds(tokenInfo.ExpiresIn));

            // Add user info to the request context
            context.Items["ModeratorId"] = tokenInfo.UserId;
            context.Items["Login"] = tokenInfo.Login;
        }

        await next(context);
    }
}

public class ValidationResponse
{
    [JsonProperty("client_id")] public string ClientId { get; set; } = string.Empty;
    [JsonProperty("login")] public string Login { get; set; } = string.Empty;
    [JsonProperty("scopes")] public List<string> Scopes { get; set; } = [];
    [JsonProperty("user_id")] public string UserId { get; set; } = string.Empty;
    [JsonProperty("expires_in")] public int ExpiresIn { get; set; }
}