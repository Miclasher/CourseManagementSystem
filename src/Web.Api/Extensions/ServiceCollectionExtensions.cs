using Microsoft.OpenApi;
using Web.Api.Infrastructure;

namespace Web.Api.Extensions;

internal static class ServiceCollectionExtensions
{
    internal static IServiceCollection AddSwaggerGenWithAuth(this IServiceCollection services)
    {
        services.AddSwaggerGen(static o =>
        {
            o.CustomSchemaIds(id => id.FullName!.Replace('+', '-'));

            // Swagger UI "Authorize" button: sets the X-User-Id header that HeaderUserContext reads.
            o.AddSecurityDefinition(HeaderUserContext.HeaderName, new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header,
                Name = HeaderUserContext.HeaderName,
                Description = "Id of the acting user (a GUID from the users table)."
            });

            o.AddSecurityRequirement(static document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(HeaderUserContext.HeaderName, document)] = []
            });
        });

        return services;
    }
}
