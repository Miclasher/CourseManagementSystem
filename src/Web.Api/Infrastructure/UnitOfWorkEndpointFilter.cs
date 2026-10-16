using Application.Abstractions.Data;

namespace Web.Api.Infrastructure;

/// <summary>
/// Commits the operations queued by repositories once the endpoint produced a non-error result,
/// before that result is written to the response. Errors and exceptions leave the queue unexecuted.
/// </summary>
internal sealed class UnitOfWorkEndpointFilter(IUnitOfWork unitOfWork) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        object? result = await next(context);

        if (result is IStatusCodeHttpResult { StatusCode: >= StatusCodes.Status400BadRequest })
        {
            return result;
        }

        await unitOfWork.CommitAsync(context.HttpContext.RequestAborted);

        return result;
    }
}
