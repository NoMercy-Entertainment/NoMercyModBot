namespace ModBot.Server.Helpers;

public static class ServiceScopeFactoryExtensions
{
    public static IServiceScope CreateRootScope(this IServiceScopeFactory factory)
    {
        return factory.CreateScope();
    }
}