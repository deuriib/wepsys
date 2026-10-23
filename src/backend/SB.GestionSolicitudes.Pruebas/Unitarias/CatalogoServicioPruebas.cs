using NSubstitute;
using Shouldly;
using SB.GestionSolicitudes.Aplicacion.Comun;
using SB.GestionSolicitudes.Aplicacion.Contratos;
using SB.GestionSolicitudes.Aplicacion.Dtos;
using SB.GestionSolicitudes.Aplicacion.Servicios;
using SB.GestionSolicitudes.Dominio.Catalogos;
using SB.GestionSolicitudes.Dominio.Usuarios;
using Xunit;

namespace SB.GestionSolicitudes.Pruebas.Unitarias;

public sealed class CatalogoServicioPruebas
{
    private readonly ICatalogoRepositorio _catalogos = Substitute.For<ICatalogoRepositorio>();
    private readonly IUnidadDeTrabajo _unidadDeTrabajo = Substitute.For<IUnidadDeTrabajo>();
    private readonly IUsuarioActual _usuarioActual = Substitute.For<IUsuarioActual>();

    [Fact]
    public async Task ActualizarArea_falla_si_el_usuario_no_es_administrador()
    {
        _usuarioActual.Rol.Returns(RolUsuario.Analista);
        var servicio = new CatalogoServicio(_catalogos, _unidadDeTrabajo, _usuarioActual);

        var resultado = await servicio.ActualizarAreaAsync(1, new ActualizarAreaRequest { Nombre = "Nueva Area" });

        resultado.Exito.ShouldBeFalse();
        resultado.Error!.Tipo.ShouldBe(TipoError.Prohibido);
    }

    [Fact]
    public async Task ActualizarArea_falla_si_el_area_no_existe()
    {
        _usuarioActual.Rol.Returns(RolUsuario.Administrador);
        _catalogos.ObtenerAreaPorIdAsync(99, Arg.Any<CancellationToken>()).Returns((Area?)null);
        var servicio = new CatalogoServicio(_catalogos, _unidadDeTrabajo, _usuarioActual);

        var resultado = await servicio.ActualizarAreaAsync(99, new ActualizarAreaRequest { Nombre = "Nueva Area" });

        resultado.Exito.ShouldBeFalse();
        resultado.Error!.Tipo.ShouldBe(TipoError.NoEncontrado);
    }

    [Fact]
    public async Task ActualizarArea_falla_si_el_nombre_esta_duplicado()
    {
        _usuarioActual.Rol.Returns(RolUsuario.Administrador);
        var area = new Area { Id = 1, Nombre = "Original", Activa = true };
        _catalogos.ObtenerAreaPorIdAsync(1, Arg.Any<CancellationToken>()).Returns(area);
        _catalogos.ExisteNombreAreaAsync("Duplicada", 1, Arg.Any<CancellationToken>()).Returns(true);
        var servicio = new CatalogoServicio(_catalogos, _unidadDeTrabajo, _usuarioActual);

        var resultado = await servicio.ActualizarAreaAsync(1, new ActualizarAreaRequest { Nombre = "Duplicada" });

        resultado.Exito.ShouldBeFalse();
        resultado.Error!.Tipo.ShouldBe(TipoError.Conflicto);
    }

    [Fact]
    public async Task ActualizarArea_actualiza_y_persiste()
    {
        _usuarioActual.Rol.Returns(RolUsuario.Administrador);
        var area = new Area { Id = 1, Nombre = "Original", Activa = true };
        _catalogos.ObtenerAreaPorIdAsync(1, Arg.Any<CancellationToken>()).Returns(area);
        _catalogos.ExisteNombreAreaAsync("Renombrada", 1, Arg.Any<CancellationToken>()).Returns(false);
        var servicio = new CatalogoServicio(_catalogos, _unidadDeTrabajo, _usuarioActual);

        var resultado = await servicio.ActualizarAreaAsync(1, new ActualizarAreaRequest { Nombre = "Renombrada", Activa = false });

        resultado.Exito.ShouldBeTrue();
        resultado.Valor!.Nombre.ShouldBe("Renombrada");
        resultado.Valor.Activa.ShouldBeFalse();
        area.Nombre.ShouldBe("Renombrada");
        area.Activa.ShouldBeFalse();
        await _unidadDeTrabajo.Received(1).GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EliminarArea_falla_si_tiene_solicitudes_asociadas()
    {
        _usuarioActual.Rol.Returns(RolUsuario.Administrador);
        var area = new Area { Id = 1, Nombre = "Tecnología", Activa = true };
        _catalogos.ObtenerAreaPorIdAsync(1, Arg.Any<CancellationToken>()).Returns(area);
        _catalogos.AreaTieneSolicitudesAsync(1, Arg.Any<CancellationToken>()).Returns(true);
        var servicio = new CatalogoServicio(_catalogos, _unidadDeTrabajo, _usuarioActual);

        var resultado = await servicio.EliminarAreaAsync(1);

        resultado.Exito.ShouldBeFalse();
        resultado.Error!.Tipo.ShouldBe(TipoError.Conflicto);
        _catalogos.DidNotReceive().EliminarArea(Arg.Any<Area>());
    }

    [Fact]
    public async Task EliminarArea_elimina_y_persiste_cuando_no_tiene_solicitudes()
    {
        _usuarioActual.Rol.Returns(RolUsuario.Administrador);
        var area = new Area { Id = 1, Nombre = "Sin Uso", Activa = true };
        _catalogos.ObtenerAreaPorIdAsync(1, Arg.Any<CancellationToken>()).Returns(area);
        _catalogos.AreaTieneSolicitudesAsync(1, Arg.Any<CancellationToken>()).Returns(false);
        var servicio = new CatalogoServicio(_catalogos, _unidadDeTrabajo, _usuarioActual);

        var resultado = await servicio.EliminarAreaAsync(1);

        resultado.Exito.ShouldBeTrue();
        _catalogos.Received(1).EliminarArea(area);
        await _unidadDeTrabajo.Received(1).GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ActualizarTipoSolicitud_actualiza_y_persiste()
    {
        _usuarioActual.Rol.Returns(RolUsuario.Administrador);
        var tipo = new TipoSolicitud { Id = 1, Nombre = "Incidente", Descripcion = "Fallo", Activo = true };
        _catalogos.ObtenerTipoSolicitudPorIdAsync(1, Arg.Any<CancellationToken>()).Returns(tipo);
        _catalogos.ExisteNombreTipoSolicitudAsync("Incidente Crítico", 1, Arg.Any<CancellationToken>()).Returns(false);
        var servicio = new CatalogoServicio(_catalogos, _unidadDeTrabajo, _usuarioActual);

        var resultado = await servicio.ActualizarTipoSolicitudAsync(1, new ActualizarTipoSolicitudRequest
        {
            Nombre = "Incidente Crítico",
            Descripcion = "Fallo mayor",
            Activo = false
        });

        resultado.Exito.ShouldBeTrue();
        resultado.Valor!.Nombre.ShouldBe("Incidente Crítico");
        resultado.Valor.Descripcion.ShouldBe("Fallo mayor");
        resultado.Valor.Activo.ShouldBeFalse();
        await _unidadDeTrabajo.Received(1).GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EliminarTipoSolicitud_falla_si_tiene_solicitudes()
    {
        _usuarioActual.Rol.Returns(RolUsuario.Administrador);
        var tipo = new TipoSolicitud { Id = 1, Nombre = "Hardware", Activo = true };
        _catalogos.ObtenerTipoSolicitudPorIdAsync(1, Arg.Any<CancellationToken>()).Returns(tipo);
        _catalogos.TipoSolicitudTieneSolicitudesAsync(1, Arg.Any<CancellationToken>()).Returns(true);
        var servicio = new CatalogoServicio(_catalogos, _unidadDeTrabajo, _usuarioActual);

        var resultado = await servicio.EliminarTipoSolicitudAsync(1);

        resultado.Exito.ShouldBeFalse();
        resultado.Error!.Tipo.ShouldBe(TipoError.Conflicto);
    }
}
