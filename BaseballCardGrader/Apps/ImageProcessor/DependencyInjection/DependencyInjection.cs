using Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace ImageProcessor.DependencyInjection;

/// <summary>
/// Extension methods for registering image processing services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers image processing services into the provided service collection.
    /// </summary>
    /// <param name="services">This should be the service collection.</param>
    /// <returns>The <see cref="IServiceCollection"/> with the registered services.</returns>
    public static IServiceCollection AddImageProcessor(this IServiceCollection services)
    {
        services.AddMediator(typeof(DependencyInjection).Assembly);
            
        return services;
    }
}