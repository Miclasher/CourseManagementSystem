using Application.Abstractions.Authentication;

namespace Web.Api.Infrastructure;

/// <summary>
/// Placeholder identity: the caller states who they are through the X-User-Id header.
/// Replace with a JWT-backed implementation when authentication is added.
/// </summary>
internal sealed class HeaderUserContext(IHttpContextAccessor httpContextAccessor) : IUserContext
{
    public const string HeaderName = "X-User-Id";

    public Guid UserId =>
        Guid.TryParse(httpContextAccessor.HttpContext?.Request.Headers[HeaderName].ToString(), out Guid userId)
            ? userId
            : throw new UnauthorizedAccessException($"The {HeaderName} header is missing or is not a valid GUID.");
}
