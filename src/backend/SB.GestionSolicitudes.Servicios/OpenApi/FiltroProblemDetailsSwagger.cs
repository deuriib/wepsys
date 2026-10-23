using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using SB.GestionSolicitudes.Servicios.Errores;

namespace SB.GestionSolicitudes.Servicios.OpenApi;

/// <summary>
/// Documents every error response with the real contract: the
/// <c>application/problem+json</c> media type and the <see cref="ProblemDetails"/> schema,
/// whatever the action declared through <c>ProducesResponseType</c>.
/// </summary>
public sealed class FiltroProblemDetailsSwagger : IOperationFilter
{
    private const int UMBRAL_ERROR = 400;

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var esquemaError = context.SchemaGenerator.GenerateSchema(typeof(ProblemDetails), context.SchemaRepository);
        var esquemaValidacion = context.SchemaGenerator.GenerateSchema(
            typeof(ValidationProblemDetails),
            context.SchemaRepository);

        foreach (var respuesta in operation.Responses)
        {
            if (!int.TryParse(respuesta.Key, out var estado) || estado < UMBRAL_ERROR)
            {
                continue;
            }

            // 400 carries the per-field `errors` member, so it documents the validation
            // shape; every other error uses the plain problem shape.
            respuesta.Value.Content = new Dictionary<string, OpenApiMediaType>
            {
                [ProblemaApi.TIPO_CONTENIDO] = new()
                {
                    Schema = estado == StatusCodes.Status400BadRequest ? esquemaValidacion : esquemaError
                }
            };
        }
    }
}
