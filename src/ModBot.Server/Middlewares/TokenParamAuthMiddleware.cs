using System.Net;
using System.Security.Claims;
using ModBot.Database;
using ModBot.Database.Models;
using ModBot.Server.Helpers;

namespace ModBot.Server.Middlewares;

public class TokenParamAuthMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {

        if (context.Request.Query.ContainsKey("token") || context.Request.Query.ContainsKey("access_token"))
        {
            string jwt = context.Request.Query
                .FirstOrDefault(q => q.Key is "token" or "access_token").Value.ToString();

            if (string.IsNullOrEmpty(jwt))
            {
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                return;
            }

            context.Request.Headers.Authorization = new("Bearer " + jwt);
        }

        await next(context);
    }
}
