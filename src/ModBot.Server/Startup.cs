using System.Security.Claims;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using ModBot.Database;
using ModBot.Database.Models;
using ModBot.Server.Config;
using ModBot.Server.Helpers;
using ModBot.Server.Hubs;
using ModBot.Server.Middlewares;
using ModBot.Server.Providers.Twitch;
using ModBot.Server.Services.Twitch;
using Newtonsoft.Json;
using RestSharp;

namespace ModBot.Server;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<TokenStore>();
        
        // services.AddSingleton<IBotClientManager, BotClientManager>();
        // services.AddSingleton<IBotManager, BotManager>();
        // services.AddHostedService(sp => (BotManager)sp.GetRequiredService<IBotManager>());
        // services.AddHostedService<BotInitializationQueue>();
        
        // services.AddDbContext<AppDbContext>(ServiceLifetime.Scoped);
        // services.AddSingleton<JsEnginesService>();
        // services.AddSingleton<TtsService>();
        // services.AddSingleton<TwitchEventSubService>();
        // services.AddSingleton<TwitchRewardsService>();
        // services.AddSingleton<LlamaService>();
        // services.AddSingleton<SevenTvApiService>();
        // services.AddSingleton<HelperService>();
        
        // Database
        services.AddSingleton<AppDbContext>();
        
        // Auth & Http
        services.AddSingleton<TwitchBotAuth>();
        services.AddSingleton<TwitchAuthService>();
        services.AddHostedService<TwitchTokenRefreshService>();
        services.AddHttpContextAccessor();

        // Twitch Services
        services.AddSingleton<TwitchApiService>();
        services.AddSingleton<TwitchChatService>();
        services.AddSingleton<TwitchChatMessagesService>();
        services.AddSingleton<TwitchCommandsService>();
        services.AddSingleton<TwitchEmotesService>();
        
        services.AddLogging(logging =>
            logging.AddSimpleConsole(options =>
            {
                options.SingleLine = true;
                options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
            })
        );
            
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
                    StringValues access_token = message.Request.Query["access_token"];
                    string[] result = access_token.ToString().Split('&');

                    if (result.Length > 0 && !string.IsNullOrEmpty(result[0]))
                    {
                        message.Request.Headers["Authorization"] = $"Bearer {result[0]}";
                    }
                            
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
                options.ClientId = Globals.TwitchClientId;
                options.ClientSecret = Globals.ClientSecret;
            });

        services.AddAuthorization();

        services.AddControllers();

        services.AddEndpointsApiExplorer();

        // SignalR with WebSockets
        services.AddHttpContextAccessor();
        services.AddSignalR(options =>
        {
            options.EnableDetailedErrors = true;
            options.MaximumReceiveMessageSize = 1024 * 1024;
            options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
            options.KeepAliveInterval = TimeSpan.FromSeconds(15);
        }).AddHubOptions<ChatHub>(options =>
        {
            options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
            options.HandshakeTimeout = TimeSpan.FromSeconds(15);
        });
        
        services.AddCors(options =>
        {
            options.AddPolicy("VueAppPolicy", builder =>
            {
                builder
                    .WithOrigins("http://localhost:5251")
                    .WithOrigins("http://192.168.2.201:5251")
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

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IServiceProvider serviceProvider)
    {
        if (env.IsDevelopment()) app.UseDeveloperExceptionPage();

        using AppDbContext dbContext = new();
        dbContext.Database.EnsureCreated();

        app.UseRouting();
        
        // Custom Middleware
        app.UseMiddleware<TokenParamAuthMiddleware>();
        
        app.UseAuthentication();
        app.UseAuthorization();
        
        app.UseForwardedHeaders(new()
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        });
        
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapHub<ChatHub>("/chatHub");
            endpoints.MapControllers();
        });

        app.UseCors("VueAppPolicy");
        
        _ = new TwitchBotAuth();
        
        if (!env.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }
        
        List<User> users = dbContext.Users
            .AsNoTracking()
            .Where(u => u.AccessToken != null && u.Username != "stoned_bot")
            .Include(u => u.ModeratorChannels)
            .ThenInclude(moderatorChannel => moderatorChannel.Broadcaster)
            .ToList();
        
        TwitchChatMessagesService twitchChat = serviceProvider.GetRequiredService<TwitchChatMessagesService>();

        using IServiceScope rootScope = app.ApplicationServices.CreateScope();

        foreach (User user in users)
        {
            if (user.AccessToken is null) continue;
            twitchChat.Connect(user, user.Username);
            
            foreach (Channel channel in user.ModeratorChannels)
            {
                twitchChat.Connect(user, channel.Broadcaster.Username);
            }
        }
        
    }
}