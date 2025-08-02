using Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace ImageProcessor.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddImageProcessor(this IServiceCollection services)
    {
        services.AddMediator(typeof(DependencyInjection).Assembly);
            
        return services;
    }
}