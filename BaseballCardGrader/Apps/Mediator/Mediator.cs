using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Mediator;

/// <summary>
/// Provides extension methods to register MyMediator services and handlers.
/// </summary>
public static class Mediator
{
    /// <summary>
    /// Registers MyMediator services and handlers from the specified assembly.
    /// </summary>
    /// <param name="services">This should be the service collection.</param>
    /// <param name="assembly">Assembly of the project.</param>
    /// <returns>The <see cref="IServiceCollection"/> with the registered mediator services and handlers.</returns>
    public static IServiceCollection AddMediator(this IServiceCollection services, Assembly? assembly = null)
    {
        // If no assembly is provided, use the calling assembly
        assembly ??= Assembly.GetCallingAssembly();

        // Register the Sender as the implementation of ISender
        services.AddScoped<ISender, Sender>();

        // Register stream request handlers
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
