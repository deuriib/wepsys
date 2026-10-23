using System.Text.Json;

namespace SB.GestionSolicitudes.Infraestructura.Archivos;

/// <summary>
/// Configuration of the plain-text database used by the government entities
/// catalog. The path is resolved to an absolute one by the composition root.
/// </summary>
public sealed class OpcionesAlmacenEntidades
{
    public const string SECCION = "AlmacenEntidadesGubernamentales";

    public string RutaArchivo { get; set; } = "database/entidades-gubernamentales.json";

    public string RutaSemilla { get; set; } = "database/entidades-gubernamentales.seed.json";

    public static readonly JsonSerializerOptions OPCIONES_JSON = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };
}

/// <summary>
/// Root document of the text database. The version field allows a future
/// migration of the file format without guessing the shape.
/// </summary>
public sealed class DocumentoEntidadesGubernamentales
{
    public const int VERSION_ACTUAL = 1;

    public int Version { get; set; } = VERSION_ACTUAL;

    public List<Dominio.EntidadesGubernamentales.EntidadGubernamental> Entidades { get; set; } = new();

    public List<string> CategoriasPersonalizadas { get; set; } = new();

    public List<string> PoderesDelEstadoPersonalizados { get; set; } = new();

    public List<string> SectoresPersonalizados { get; set; } = new();
}
