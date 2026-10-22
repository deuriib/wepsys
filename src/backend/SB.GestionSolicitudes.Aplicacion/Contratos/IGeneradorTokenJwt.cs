using SB.GestionSolicitudes.Dominio.Usuarios;

namespace SB.GestionSolicitudes.Aplicacion.Contratos;

/// <summary>
/// A signed JWT plus the instant it stops being valid.
/// </summary>
public sealed record TokenGenerado(string Token, DateTime ExpiraEn);

/// <summary>
/// Token generation contract. The signing key is never known by the application
/// layer, which keeps secret material inside infrastructure configuration.
/// </summary>
public interface IGeneradorTokenJwt
{
    TokenGenerado Generar(Usuario usuario);
}
