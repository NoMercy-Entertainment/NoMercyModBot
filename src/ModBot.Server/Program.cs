using System.Net;
using Microsoft.AspNetCore;
using Microsoft.EntityFrameworkCore;
using ModBot.Database;

namespace ModBot.Server;

public static class Program
{
    public static Task Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) =>
        {
            Exception exception = (Exception)eventArgs.ExceptionObject;
        };

        Console.CancelKeyPress += (_, _) => { Environment.Exit(0); };

        AppDomain.CurrentDomain.ProcessExit += (_, _) => { Environment.Exit(0); };

        return Start(args);
    }

    private static Task Start(string[] options)
    {
        Console.Clear();
        Console.Title = "NoMercy Server";

        IWebHost app = CreateWebHostBuilder(new WebHostBuilder()).Build();
        
        using IServiceScope rootScope = app.Services.CreateScope();
        rootScope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
        
        return app.RunAsync();
    }

    private static IWebHostBuilder CreateWebHostBuilder(this IWebHostBuilder _)
    {
        UriBuilder localhostIPv4Url = new()
        {
            Host = IPAddress.Any.ToString(),
            Port = 5251,
            Scheme = Uri.UriSchemeHttp
        };

        List<string> urls = [localhostIPv4Url.ToString()];

        return WebHost.CreateDefaultBuilder([])
            .UseUrls(urls.ToArray())
            .UseKestrel(options =>
            {
                options.AddServerHeader = false;
                options.Limits.MaxRequestBodySize = null;
                options.Limits.MaxRequestBufferSize = null;
                options.Limits.MaxConcurrentConnections = null;
                options.Limits.MaxConcurrentUpgradedConnections = null;
            })
            .UseQuic()
            .UseSockets()
            .UseStartup<Startup>();
    }
}