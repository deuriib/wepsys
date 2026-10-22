using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Shouldly;
using SB.GestionSolicitudes.Aplicacion.Contratos;
using SB.GestionSolicitudes.Dominio.Usuarios;
using SB.GestionSolicitudes.Infraestructura.Configuracion;
using SB.GestionSolicitudes.Infraestructura.Seguridad;
using Xunit;

namespace SB.GestionSolicitudes.Pruebas.Unitarias;

/// <summary>
/// The token is the only thing the client holds, so its claims and expiry are
/// part of the security contract.
/// </summary>
public sealed class GeneradorTokenJwtPruebas
{
    private const string CLAVE = "clave-de-pruebas-unitarias-0123456789abcdef";

    [Fact]
    public void El_token_incluye_identidad_correo_y_rol()
    {
        var usuario = new Usuario
        {
            Id = 42,
            Nombre = "Ana Martínez",
            Correo = "ana@sb.gob.do",
            Rol = RolUsuario.Administrador,
            Activo = true
        };

        var generado = CrearGenerador().Generar(usuario);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(generado.Token);

        token.Issuer.ShouldBe("SB.GestionSolicitudes");
        token.Audiences.ShouldContain("SB.GestionSolicitudes.Cliente");
        token.Claims.Single(claim => claim.Type == ClaimTypes.NameIdentifier).Value.ShouldBe("42");
        token.Claims.Single(claim => claim.Type == ClaimTypes.Email).Value.ShouldBe("ana@sb.gob.do");
        token.Claims.Single(claim => claim.Type == ClaimTypes.Role).Value.ShouldBe("Administrador");
    }

    [Fact]
    public void La_expiracion_respeta_los_minutos_configurados()
    {
        var ahora = new DateTime(2026, 10, 22, 12, 0, 0, DateTimeKind.Utc);

        var generado = CrearGenerador(ahora, minutos: 45).Generar(CrearUsuario());

        generado.ExpiraEn.ShouldBe(ahora.AddMinutes(45));
        new JwtSecurityTokenHandler().ReadJwtToken(generado.Token).ValidTo.ShouldBe(ahora.AddMinutes(45));
    }

    [Fact]
    public void El_token_esta_firmado_con_HMACSHA256()
    {
        var generado = CrearGenerador().Generar(CrearUsuario());
        var token = new JwtSecurityTokenHandler().ReadJwtToken(generado.Token);

        token.Header.Alg.ShouldBe("HS256");
        token.RawData.Split('.')[2].ShouldNotBeNullOrWhiteSpace();
    }

    private static GeneradorTokenJwt CrearGenerador(DateTime? ahora = null, int minutos = 120) =>
        new(
            Options.Create(new OpcionesJwt
            {
                Emisor = "SB.GestionSolicitudes",
                Audiencia = "SB.GestionSolicitudes.Cliente",
                ClaveSecreta = CLAVE,
                MinutosVigencia = minutos
            }),
            new RelojFijo(ahora ?? new DateTime(2026, 10, 22, 12, 0, 0, DateTimeKind.Utc)));

    private static Usuario CrearUsuario() => new()
    {
        Id = 7,
        Nombre = "Luis Peña",
        Correo = "luis@sb.gob.do",
        Rol = RolUsuario.Analista,
        Activo = true
    };

    private sealed class RelojFijo : IReloj
    {
        public RelojFijo(DateTime ahora)
        {
            Ahora = ahora;
        }

        public DateTime Ahora { get; }
    }
}
