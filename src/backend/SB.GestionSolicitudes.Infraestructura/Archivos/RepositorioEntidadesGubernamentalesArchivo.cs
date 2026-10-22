namespace SB.GestionSolicitudes.Infraestructura.Archivos;

using SB.GestionSolicitudes.Aplicacion.Comun;
using SB.GestionSolicitudes.Aplicacion.Contratos;
using SB.GestionSolicitudes.Aplicacion.Dtos;
using SB.GestionSolicitudes.Dominio.EntidadesGubernamentales;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Plain-text implementation of the government entities catalog: a single JSON
/// document inside <c>database/</c>, exactly as the first specification asks.
/// Writes are serialized with a semaphore and committed through a temporary
/// file + atomic replace, so a crash never leaves a half-written database.
/// </summary>
public sealed class RepositorioEntidadesGubernamentalesArchivo : IEntidadGubernamentalRepositorio, IDisposable
{
    private const string SUFIJO_TEMPORAL = ".tmp";

    private readonly SemaphoreSlim _semaforo = new(1, 1);
    private readonly OpcionesAlmacenEntidades _opciones;
    private readonly ILogger<RepositorioEntidadesGubernamentalesArchivo> _registro;
    private DocumentoEntidadesGubernamentales? _documento;

    public RepositorioEntidadesGubernamentalesArchivo(
        IOptions<OpcionesAlmacenEntidades> opciones,
        ILogger<RepositorioEntidadesGubernamentalesArchivo> registro)
    {
        _opciones = opciones.Value;
        _registro = registro;
    }

    public async Task<ResultadoPaginado<EntidadGubernamental>> ConsultarAsync(
        FiltroEntidadGubernamental filtro,
        ParametrosPaginacion paginacion,
        CancellationToken cancellationToken = default)
    {
        await _semaforo.WaitAsync(cancellationToken);

        try
        {
            var documento = await ObtenerDocumentoAsync(cancellationToken);
            var consulta = documento.Entidades.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(filtro.Texto))
            {
                var texto = filtro.Texto.Trim();
                consulta = consulta.Where(entidad =>
                    entidad.Nombre.Contains(texto, StringComparison.OrdinalIgnoreCase) ||
                    entidad.Categoria.Contains(texto, StringComparison.OrdinalIgnoreCase) ||
                    entidad.Sector.Contains(texto, StringComparison.OrdinalIgnoreCase) ||
                    entidad.PoderDelEstado.Contains(texto, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(filtro.Categoria))
            {
                consulta = consulta.Where(entidad =>
                    entidad.Categoria.Equals(filtro.Categoria.Trim(), StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(filtro.PoderDelEstado))
            {
                consulta = consulta.Where(entidad =>
                    entidad.PoderDelEstado.Equals(filtro.PoderDelEstado.Trim(), StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(filtro.Sector))
            {
                consulta = consulta.Where(entidad =>
                    entidad.Sector.Equals(filtro.Sector.Trim(), StringComparison.OrdinalIgnoreCase));
            }

            if (filtro.Activa is not null)
            {
                consulta = consulta.Where(entidad => entidad.Activa == filtro.Activa.Value);
            }

            var ordenada = consulta
                .OrderBy(entidad => entidad.Nombre, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var elementos = ordenada
                .Skip(paginacion.Omitir)
                .Take(paginacion.TamanoNormalizado)
                .ToList();

            return new ResultadoPaginado<EntidadGubernamental>(
                elementos,
                ordenada.Count,
                paginacion.PaginaNormalizada,
                paginacion.TamanoNormalizado);
        }
        finally
        {
            _semaforo.Release();
        }
    }

    public async Task<EntidadGubernamental?> ObtenerPorIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await _semaforo.WaitAsync(cancellationToken);

        try
        {
            var documento = await ObtenerDocumentoAsync(cancellationToken);
            return documento.Entidades.FirstOrDefault(entidad => entidad.Id == id);
        }
        finally
        {
            _semaforo.Release();
        }
    }

    public async Task<bool> ExisteNombreAsync(
        string nombre,
        Guid? excluirId,
        CancellationToken cancellationToken = default)
    {
        await _semaforo.WaitAsync(cancellationToken);

        try
        {
            var documento = await ObtenerDocumentoAsync(cancellationToken);
            return documento.Entidades.Any(entidad =>
                entidad.Nombre.Equals(nombre.Trim(), StringComparison.OrdinalIgnoreCase) &&
                entidad.Id != excluirId);
        }
        finally
        {
            _semaforo.Release();
        }
    }

    public async Task AgregarAsync(EntidadGubernamental entidad, CancellationToken cancellationToken = default)
    {
        await _semaforo.WaitAsync(cancellationToken);

        try
        {
            var documento = await ObtenerDocumentoAsync(cancellationToken);
            documento.Entidades.Add(entidad);
            await PersistirAsync(documento, cancellationToken);
        }
        finally
        {
            _semaforo.Release();
        }
    }

    public async Task ActualizarAsync(EntidadGubernamental entidad, CancellationToken cancellationToken = default)
    {
        await _semaforo.WaitAsync(cancellationToken);

        try
        {
            var documento = await ObtenerDocumentoAsync(cancellationToken);
            var indice = documento.Entidades.FindIndex(item => item.Id == entidad.Id);

            if (indice < 0)
            {
                throw new InvalidOperationException($"No existe la entidad gubernamental {entidad.Id} en el archivo de datos.");
            }

            documento.Entidades[indice] = entidad;
            await PersistirAsync(documento, cancellationToken);
        }
        finally
        {
            _semaforo.Release();
        }
    }

    public async Task<bool> EliminarAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _semaforo.WaitAsync(cancellationToken);

        try
        {
            var documento = await ObtenerDocumentoAsync(cancellationToken);
            var eliminadas = documento.Entidades.RemoveAll(entidad => entidad.Id == id);

            if (eliminadas == 0)
            {
                return false;
            }

            await PersistirAsync(documento, cancellationToken);
            return true;
        }
        finally
        {
            _semaforo.Release();
        }
    }

    public async Task<int> ContarAsync(CancellationToken cancellationToken = default)
    {
        await _semaforo.WaitAsync(cancellationToken);

        try
        {
            var documento = await ObtenerDocumentoAsync(cancellationToken);
            return documento.Entidades.Count;
        }
        finally
        {
            _semaforo.Release();
        }
    }

    public async Task AgregarRangoAsync(
        IEnumerable<EntidadGubernamental> entidades,
        CancellationToken cancellationToken = default)
    {
        await _semaforo.WaitAsync(cancellationToken);

        try
        {
            var documento = await ObtenerDocumentoAsync(cancellationToken);
            documento.Entidades.AddRange(entidades);
            await PersistirAsync(documento, cancellationToken);
        }
        finally
        {
            _semaforo.Release();
        }
    }

    public async Task<OpcionesFiltroEntidadesDto> ObtenerOpcionesFiltroAsync(
        CancellationToken cancellationToken = default)
    {
        await _semaforo.WaitAsync(cancellationToken);

        try
        {
            var documento = await ObtenerDocumentoAsync(cancellationToken);

            return new OpcionesFiltroEntidadesDto(
                documento.Entidades.Select(entidad => entidad.Categoria).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(valor => valor).ToList(),
                documento.Entidades.Select(entidad => entidad.PoderDelEstado).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(valor => valor).ToList(),
                documento.Entidades.Select(entidad => entidad.Sector).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(valor => valor).ToList());
        }
        finally
        {
            _semaforo.Release();
        }
    }

    public void Dispose() => _semaforo.Dispose();

    private async Task<DocumentoEntidadesGubernamentales> ObtenerDocumentoAsync(CancellationToken cancellationToken)
    {
        if (_documento is not null)
        {
            return _documento;
        }

        if (!File.Exists(_opciones.RutaArchivo))
        {
            await CrearDesdeSemillaAsync(cancellationToken);
        }

        var contenido = await File.ReadAllTextAsync(_opciones.RutaArchivo, cancellationToken);

        try
        {
            _documento = JsonSerializer.Deserialize<DocumentoEntidadesGubernamentales>(
                contenido,
                OpcionesAlmacenEntidades.OPCIONES_JSON) ?? new DocumentoEntidadesGubernamentales();
        }
        catch (JsonException excepcion)
        {
            throw new InvalidOperationException(
                $"El archivo de datos '{_opciones.RutaArchivo}' no contiene un JSON válido.",
                excepcion);
        }

        return _documento;
    }

    private async Task CrearDesdeSemillaAsync(CancellationToken cancellationToken)
    {
        var directorio = Path.GetDirectoryName(_opciones.RutaArchivo);

        if (!string.IsNullOrWhiteSpace(directorio))
        {
            Directory.CreateDirectory(directorio);
        }

        if (File.Exists(_opciones.RutaSemilla))
        {
            var contenidoSemilla = await File.ReadAllTextAsync(_opciones.RutaSemilla, cancellationToken);
            await File.WriteAllTextAsync(_opciones.RutaArchivo, contenidoSemilla, cancellationToken);
            _registro.LogInformation(
                "Base de datos de entidades gubernamentales creada desde la semilla {Semilla} en {Ruta}.",
                _opciones.RutaSemilla,
                _opciones.RutaArchivo);
            return;
        }

        var documentoVacio = new DocumentoEntidadesGubernamentales();
        await File.WriteAllTextAsync(
            _opciones.RutaArchivo,
            JsonSerializer.Serialize(documentoVacio, OpcionesAlmacenEntidades.OPCIONES_JSON),
            cancellationToken);

        _registro.LogWarning(
            "No se encontró la semilla {Semilla}; se creó una base de datos de entidades vacía en {Ruta}.",
            _opciones.RutaSemilla,
            _opciones.RutaArchivo);
    }

    private async Task PersistirAsync(
        DocumentoEntidadesGubernamentales documento,
        CancellationToken cancellationToken)
    {
        var rutaTemporal = _opciones.RutaArchivo + SUFIJO_TEMPORAL;
        var contenido = JsonSerializer.Serialize(documento, OpcionesAlmacenEntidades.OPCIONES_JSON);

        await File.WriteAllTextAsync(rutaTemporal, contenido, cancellationToken);
        File.Move(rutaTemporal, _opciones.RutaArchivo, overwrite: true);
    }
}
