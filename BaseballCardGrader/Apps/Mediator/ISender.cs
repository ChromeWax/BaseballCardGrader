namespace Mediator;

/// <summary>
/// Defines a mediator interface for sending requests and creating streams.
/// </summary>
public interface ISender
{
    /// <summary>
    /// Sends a request by resolving the appropriate <see cref="IRequestHandler{TRequest, TResponse}"/> from the service provider.
    /// </summary>
    /// <param name="request">The type of request being handled.</param>
    /// <param name="cancellationToken">Cancellation token to cancel operation if invoked.</param>
    /// <typeparam name="TResponse">The type of response returned by handler.</typeparam>
    /// <returns>Response of type <see cref="TResponse"/>.</returns>
    Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
}