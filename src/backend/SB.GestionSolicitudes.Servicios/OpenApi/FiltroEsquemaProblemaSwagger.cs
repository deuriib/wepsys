using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using SB.GestionSolicitudes.Servicios.Errores;

namespace SB.GestionSolicitudes.Servicios.OpenApi;

/// <summary>
/// Adds the two extension members this API guarantees to the documented problem schema,
/// so Swagger shows the full contract and not only the RFC 9457 core members.
/// </summary>
public sealed class FiltroEsquemaProblemaSwagger : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type != typeof(ProblemDetails) && context.Type != typeof(ValidationProblemDetails))
        {
            return;
        }

        schema.Properties[ProblemaApi.CLAVE_CODIGO] = new OpenApiSchema
        {
            Type = "string",
            Description = "Código de negocio estable (por ejemplo VALIDACION o NO_ENCONTRADO)."
        };

        schema.Properties[ProblemaApi.CLAVE_TRAZABILIDAD] = new OpenApiSchema
        {
            Type = "string",
            Description = "Identificador de correlación que coincide con la cabecera X-Trace-Id y con los logs."
        };
    }
}
