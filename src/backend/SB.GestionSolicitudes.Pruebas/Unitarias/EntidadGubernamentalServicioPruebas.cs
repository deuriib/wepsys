using NSubstitute;
using Shouldly;
using SB.GestionSolicitudes.Aplicacion.Comun;
using SB.GestionSolicitudes.Aplicacion.Contratos;
using SB.GestionSolicitudes.Aplicacion.Servicios;
using SB.GestionSolicitudes.Dominio.Usuarios;
using Xunit;

namespace SB.GestionSolicitudes.Pruebas.Unitarias;

public sealed class EntidadGubernamentalServicioPruebas
{
    private readonly IEntidadGubernamentalRepositorio _repositorio = Substitute.For<IEntidadGubernamentalRepositorio>();
    private readonly IUsuarioActual _usuarioActual = Substitute.For<IUsuarioActual>();
    private readonly IReloj _reloj = Substitute.For<IReloj>();

    [Fact]
    public async Task AgregarOpcionCatalogo_falla_si_no_es_administrador_o_analista()
    {
        _usuarioActual.Rol.Returns(RolUsuario.Solicitante);
        var servicio = new EntidadGubernamentalServicio(_repositorio, _usuarioActual, _reloj);

        var resultado = await servicio.AgregarOpcionCatalogoAsync("categorias", "Nueva Categoria");

        resultado.Exito.ShouldBeFalse();
        resultado.Error!.Tipo.ShouldBe(TipoError.Prohibido);
    }

    [Fact]
    public async Task AgregarOpcionCatalogo_agrega_exitosamente()
    {
        _usuarioActual.Rol.Returns(RolUsuario.Administrador);
        _repositorio.AgregarOpcionCatalogoAsync("categorias", "Nueva Categoria", Arg.Any<CancellationToken>())
            .Returns(true);
        var servicio = new EntidadGubernamentalServicio(_repositorio, _usuarioActual, _reloj);

        var resultado = await servicio.AgregarOpcionCatalogoAsync("categorias", "Nueva Categoria");

        resultado.Exito.ShouldBeTrue();
    }

    [Fact]
    public async Task AgregarOpcionCatalogo_retorna_conflicto_si_esta_duplicada()
    {
        _usuarioActual.Rol.Returns(RolUsuario.Administrador);
        _repositorio.AgregarOpcionCatalogoAsync("categorias", "Existente", Arg.Any<CancellationToken>())
            .Returns(false);
        var servicio = new EntidadGubernamentalServicio(_repositorio, _usuarioActual, _reloj);

        var resultado = await servicio.AgregarOpcionCatalogoAsync("categorias", "Existente");

        resultado.Exito.ShouldBeFalse();
        resultado.Error!.Tipo.ShouldBe(TipoError.Conflicto);
    }

    [Fact]
    public async Task EliminarOpcionCatalogo_retorna_conflicto_si_esta_en_uso()
    {
        _usuarioActual.Rol.Returns(RolUsuario.Administrador);
        _repositorio.EliminarOpcionCatalogoAsync("sectores", "Finanzas", Arg.Any<CancellationToken>())
            .Returns(false);
        var servicio = new EntidadGubernamentalServicio(_repositorio, _usuarioActual, _reloj);

        var resultado = await servicio.EliminarOpcionCatalogoAsync("sectores", "Finanzas");

        resultado.Exito.ShouldBeFalse();
        resultado.Error!.Tipo.ShouldBe(TipoError.Conflicto);
    }

    [Fact]
    public async Task EliminarOpcionCatalogo_elimina_cuando_no_esta_en_uso()
    {
        _usuarioActual.Rol.Returns(RolUsuario.Administrador);
        _repositorio.EliminarOpcionCatalogoAsync("sectores", "Sin Uso", Arg.Any<CancellationToken>())
            .Returns(true);
        var servicio = new EntidadGubernamentalServicio(_repositorio, _usuarioActual, _reloj);

        var resultado = await servicio.EliminarOpcionCatalogoAsync("sectores", "Sin Uso");

        resultado.Exito.ShouldBeTrue();
    }
}
