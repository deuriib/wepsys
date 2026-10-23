using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;
using Serilog;
using SB.GestionSolicitudes.Aplicacion.Contratos;
using SB.GestionSolicitudes.Infraestructura;
using SB.GestionSolicitudes.Infraestructura.Configuracion;
using SB.GestionSolicitudes.Infraestructura.Semillas;
using SB.GestionSolicitudes.Servicios;
using SB.GestionSolicitudes.Servicios.Autenticacion;
using SB.GestionSolicitudes.Servicios.Errores;
using SB.GestionSolicitudes.Servicios.Middleware;
using SB.GestionSolicitudes.Servicios.OpenApi;

const string NOMBRE_API = "SB · Gestión de Solicitudes Internas";
const string CLAVE_DESARROLLO = "clave-solo-para-desarrollo-local-0123456789abcdef";
const int LONGITUD_MINIMA_CLAVE = 32;
const string POLITICA_CORS = "ClienteWeb";
const string ORIGENES_PREDETERMINADOS = "http://localhost:5173,http://localhost:4173";

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((contexto, configuracion) => configuracion
    .ReadFrom.Configuration(contexto.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Aplicacion", "SB.GestionSolicitudes"));

builder.Services.AgregarInfraestructura(builder.Configuration, builder.Environment.ContentRootPath);
builder.Services.AgregarAplicacion();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUsuarioActual, UsuarioActual>();

builder.Services
    .AddControllers()
    .AddJsonOptions(opciones =>
        opciones.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.Configure<ApiBehaviorOptions>(opciones =>
{
    // Model validation returns the same RFC 9457 contract as the rest of the API,
    // with the per-field messages in the standard `errors` member.
    opciones.InvalidModelStateResponseFactory = contextoAccion =>
    {
        var errores = contextoAccion.ModelState
            .Where(entrada => entrada.Value?.Errors.Count > 0)
            .ToDictionary(
                entrada => entrada.Key,
                entrada => entrada.Value!.Errors
                    .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage)
                        ? "Valor inválido."
                        : error.ErrorMessage)
                    .ToArray());

        return ProblemaApi.ComoResultado(
            ProblemaApi.CrearValidacion(errores, contextoAccion.HttpContext));
    };
});

// Problem details is the single error contract. The exception handler, the status code
// pages and the validation factory build their payloads with ProblemaApi; this
// registration keeps framework-authored problems (for example Results.Problem) aligned
// with the same `codigo` and `trazabilidad` extensions.
builder.Services.AddProblemDetails(opciones =>
    opciones.CustomizeProblemDetails = ProblemaApi.CompletarConTrazabilidad);

builder.Services.AddExceptionHandler<ManejadorExcepciones>();

// The secret is resolved when the options are built, not while this file runs, so
// integration tests and deployed environments can supply it through the host
// configuration. In Production a missing key fails at startup, never per request.
builder.Services.AddOptions<OpcionesJwt>()
    .Bind(builder.Configuration.GetSection(OpcionesJwt.SECCION))
    .PostConfigure(opciones => ResolverClaveSecreta(opciones, builder.Environment))
    .ValidateOnStart();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

builder.Services
    .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<OpcionesJwt>>((opciones, jwt) =>
    {
        var configuracion = jwt.Value;

        // Claims are written with their full URI names, so we disable the inbound
        // mapping to keep generation and validation symmetrical.
        opciones.MapInboundClaims = false;
        opciones.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = configuracion.Emisor,
            ValidateAudience = true,
            ValidAudience = configuracion.Audiencia,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuracion.ClaveSecreta)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(opciones => opciones.AddPolicy(
    POLITICA_CORS,
    politica =>
    {
        var origenes = (builder.Configuration["Cors:Origenes"] ?? ORIGENES_PREDETERMINADOS)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        politica.WithOrigins(origenes).AllowAnyHeader().AllowAnyMethod();
    }));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opciones =>
{
    opciones.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = NOMBRE_API,
        Version = "v1",
        Description =
            "API para registrar, clasificar y dar seguimiento a solicitudes internas de servicios tecnológicos. " +
            "Incluye autenticación JWT por roles, historial de estados, comentarios, notificaciones y tablero de indicadores. " +
            "El mantenimiento de entidades gubernamentales se persiste en un archivo de texto plano dentro del proyecto.",
        Contact = new OpenApiContact
        {
            Name = "Superintendencia de Bancos de la República Dominicana",
            Url = new Uri("https://www.sb.gob.do")
        }
    });

    var archivosDocumentacion = Directory.GetFiles(AppContext.BaseDirectory, "SB.GestionSolicitudes.*.xml");
    foreach (var archivo in archivosDocumentacion)
    {
        opciones.IncludeXmlComments(archivo, includeControllerXmlComments: true);
    }

    opciones.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Token obtenido en POST /api/auth/login. Ejemplo: Bearer {token}"
    });

    opciones.OperationFilter<FiltroSeguridadSwagger>();

    // Registered last so it also normalizes the 401 response added by the security filter.
    opciones.OperationFilter<FiltroProblemDetailsSwagger>();
    opciones.SchemaFilter<FiltroEsquemaProblemaSwagger>();
});

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseSerilogRequestLogging(opciones =>
{
    opciones.MessageTemplate =
        "HTTP {RequestMethod} {RequestPath} respondió {StatusCode} en {Elapsed:0.0000} ms";
});

// Every failure leaves the API as an RFC 9457 problem document. The exception handler
// covers thrown errors; the status code pages cover responses produced without a body
// (JWT challenge, unknown route, unsupported media type) so no caller receives an empty
// error response.
app.UseExceptionHandler();
app.UseStatusCodePages(async contexto =>
{
    var http = contexto.HttpContext;
    var (codigo, titulo, detalle) = ProblemaApi.DescribirEstado(http.Response.StatusCode);

    await ProblemaApi.EscribirAsync(
        http,
        ProblemaApi.Crear(http.Response.StatusCode, codigo, titulo, detalle, http));
});

app.UseCors(POLITICA_CORS);

if (app.Configuration.GetValue("Documentacion:Habilitada", true))
{
    app.UseSwagger();
    app.UseSwaggerUI(opciones =>
    {
        opciones.SwaggerEndpoint("/swagger/v1/swagger.json", NOMBRE_API);
        opciones.RoutePrefix = "swagger";
        opciones.DocumentTitle = NOMBRE_API;
    });

    // Scalar is a second, read-only UI over the same OpenAPI document.
    app.MapScalarApiReference(opciones => opciones
        .WithEndpointPrefix("/docs/{documentName}")
        .WithTitle(NOMBRE_API)
        .WithOpenApiRoutePattern("/swagger/{documentName}/swagger.json"));
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/salud", new HealthCheckOptions { AllowCachingResponses = false }).AllowAnonymous();

await SemillaDatos.InicializarAsync(app.Services);

app.Run();

static void ResolverClaveSecreta(OpcionesJwt opciones, IWebHostEnvironment entorno)
{
    if (!string.IsNullOrWhiteSpace(opciones.ClaveSecreta) && opciones.ClaveSecreta.Length >= LONGITUD_MINIMA_CLAVE)
    {
        return;
    }

    if (!entorno.IsDevelopment())
    {
        throw new InvalidOperationException(
            $"Configure 'Jwt:ClaveSecreta' (variable de entorno Jwt__ClaveSecreta) con al menos {LONGITUD_MINIMA_CLAVE} caracteres.");
    }

    opciones.ClaveSecreta = CLAVE_DESARROLLO;
}

/// <summary>
/// Exposed so the integration tests can boot the API with WebApplicationFactory.
/// </summary>
public partial class Program
{
}
