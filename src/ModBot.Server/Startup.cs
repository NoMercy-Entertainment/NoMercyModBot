using System.Security.Claims;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Primitives;
using ModBot.Database;
using ModBot.Server.Config;
using ModBot.Server.Helpers;
using ModBot.Server.Middlewares;
using Newtonsoft.Json;
using RestSharp;

namespace ModBot.Server;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<AppDbContext>();

        services.AddMemoryCache();

        services.AddControllers()
            .AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            })
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

        services.AddOpenApi();

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "BearerToken";
                options.DefaultChallengeScheme = "BearerToken";
                options.DefaultSignInScheme = "BearerToken";
            })
            .AddBearerToken(options =>
            {
                // ReSharper disable once RedundantDelegateCreation
                options.Events.OnMessageReceived = new(async message =>
                {
                    if (!message.Request.Headers.TryGetValue("Authorization", out StringValues authHeader))
                    {
                        message.Fail("No authorization header");
                        await Task.CompletedTask;
                    }

                    string? accessToken = authHeader.ToString().Split("Bearer ").LastOrDefault();
                    if (string.IsNullOrEmpty(accessToken))
                    {
                        message.Fail("No token provided");
                        await Task.CompletedTask;
                    }

                    RestClient client = new($"{Globals.TwitchAuthUrl}/validate");
                    RestRequest request = new();
                    request.AddHeader("Authorization", $"OAuth {accessToken}");

                    RestResponse response = await client.ExecuteAsync(request);
                    if (!response.IsSuccessful)
                    {
                        message.Fail("Failed to validate token");
                        await Task.CompletedTask;
                    }

                    ValidationResponse? user = response.Content?.FromJson<ValidationResponse>();
                    if (user == null)
                    {
                        message.Fail("Invalid token");
                        await Task.CompletedTask;
                    }

                    message.HttpContext.User = new(new ClaimsIdentity([
                        new(ClaimTypes.NameIdentifier, user!.UserId),
                        new(ClaimTypes.Name, user.Login)
                    ], "BearerToken"));

                    await Task.CompletedTask;
                });
            })
            .AddTwitch(options =>
            {
                Console.WriteLine("Twitch authentication configured");
                options.ClientId = Globals.TwitchClientId;
                options.ClientSecret = Globals.ClientSecret;
            });

        services.AddAuthorization();

        services.AddControllers();

        services.AddEndpointsApiExplorer();

        services.AddCors(options =>
        {
            options.AddPolicy("VueAppPolicy", builder =>
            {
                builder
                    .WithOrigins("http://localhost:5251")
                    .WithOrigins("https://modbot.nomercy.tv")
                    .AllowAnyMethod()
                    .AllowCredentials()
                    .SetIsOriginAllowedToAllowWildcardSubdomains()
                    .WithHeaders("Access-Control-Allow-Private-Network", "true")
                    .WithHeaders("Access-Control-Allow-Headers", "*")
                    .AllowAnyHeader();
            });
        });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment()) app.UseDeveloperExceptionPage();

        using AppDbContext dbContext = new();
        dbContext.Database.EnsureCreated();

        app.UseRouting();

        app.UseForwardedHeaders(new()
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        });

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints => { endpoints.MapControllers(); });

        // Add before app.Run()
        app.UseCors("VueAppPolicy");
    }
}