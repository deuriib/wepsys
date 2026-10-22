using Shouldly;
using SB.GestionSolicitudes.Infraestructura.Seguridad;
using Xunit;

namespace SB.GestionSolicitudes.Pruebas.Unitarias;

/// <summary>
/// Password storage must be salted, verifiable and never reversible. These tests
/// pin those properties so a refactor cannot silently weaken them.
/// </summary>
public sealed class ServicioHashContrasenaPruebas
{
    private const string CONTRASENA = "Solicitud123!";

    private readonly ServicioHashContrasena _servicio = new();

    [Fact]
    public void El_hash_no_contiene_la_contrasena_en_claro()
    {
        var hash = _servicio.Hashear(CONTRASENA);

        hash.ShouldNotContain(CONTRASENA);
        hash.Split('.').Length.ShouldBe(3);
    }

    [Fact]
    public void La_misma_contrasena_produce_hashes_distintos_por_la_sal_aleatoria()
    {
        var primero = _servicio.Hashear(CONTRASENA);
        var segundo = _servicio.Hashear(CONTRASENA);

        primero.ShouldNotBe(segundo);
        _servicio.Verificar(CONTRASENA, primero).ShouldBeTrue();
        _servicio.Verificar(CONTRASENA, segundo).ShouldBeTrue();
    }

    [Fact]
    public void Una_contrasena_incorrecta_no_verifica()
    {
        var hash = _servicio.Hashear(CONTRASENA);

        _servicio.Verificar("Solicitud123?", hash).ShouldBeFalse();
        _servicio.Verificar(string.Empty, hash).ShouldBeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("sin-separadores")]
    [InlineData("abc.ZZZ.def")]
    [InlineData("120000.###.def")]
    public void Un_hash_manipulado_no_lanza_excepcion_ni_verifica(string hashManipulado)
    {
        _servicio.Verificar(CONTRASENA, hashManipulado).ShouldBeFalse();
    }

    [Fact]
    public void Hashear_rechaza_una_contrasena_vacia()
    {
        Should.Throw<ArgumentException>(() => _servicio.Hashear("  "));
    }
}
