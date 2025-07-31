using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace MyMediator;

public static class MyMediator
{
    public static IServiceCollection AddMyMediator(this IServiceCollection services, Assembly? assembly = null)
    {
        assembly ??= Assembly.GetCallingAssembly();

        services.AddScoped<ISender, Sender>();

        var requestHandlerInterfaceType = typeof(IRequestHandler<,>);
        var requestHandlerTypes = assembly
            .GetTypes()
            .Where(type => !type.IsAbstract && !type.IsInterface)
            .SelectMany(type => type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == requestHandlerInterfaceType)
                .Select(i => new { Interface = i, Implementation = type }));
        foreach (var handler in requestHandlerTypes)
        {
            services.AddScoped(handler.Interface, handler.Implementation);
        }
        
        return services;
    }
}
