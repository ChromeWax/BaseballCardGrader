using Microsoft.Extensions.DependencyInjection;

namespace Mediator;

/// <summary>
/// Default implementation of <see cref="ISender"/> that resolves handlers from the provided <see cref="IServiceProvider"/>.
/// </summary>
/// <param name="provider">The service provider.</param>
internal class Sender(IServiceProvider provider) : ISender
{
    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(request.GetType(), typeof(TResponse));
        dynamic handler = provider.GetRequiredService(handlerType);
        return handler.Handle((dynamic)request, cancellationToken);
    }
}
