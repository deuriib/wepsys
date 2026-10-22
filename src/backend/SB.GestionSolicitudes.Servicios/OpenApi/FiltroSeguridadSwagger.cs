using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using SB.GestionSolicitudes.Servicios.Errores;

namespace SB.GestionSolicitudes.Servicios.OpenApi;

/// <summary>
/// Applies the JWT requirement to every operation except the anonymous ones
/// (login, health). This keeps the Swagger UI honest about which calls need a token.
/// </summary>
public sealed class FiltroSeguridadSwagger : IOperationFilter
{
    private const string ESQUEMA = "Bearer";

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var permiteAnonimo = context.ApiDescription.ActionDescriptor.EndpointMetadata
            .OfType<Microsoft.AspNetCore.Authorization.IAllowAnonymous>()
            .Any();

        if (permiteAnonimo)
        {
            return;
        }

        operation.Responses.TryAdd("401", new OpenApiResponse
        {
            Description = "No autenticado o token inválido.",
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["application/json"] = new() { Schema = context.SchemaGenerator.GenerateSchema(typeof(RespuestaError), context.SchemaRepository) }
            }
        });

        operation.Security = new List<OpenApiSecurityRequirement>
        {
            new()
            {
                [
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = ESQUEMA }
                    }
                ] = Array.Empty<string>()
            }
        };
    }
}
