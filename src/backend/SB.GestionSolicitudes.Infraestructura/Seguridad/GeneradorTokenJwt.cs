using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SB.GestionSolicitudes.Aplicacion.Contratos;
using SB.GestionSolicitudes.Dominio.Usuarios;
using SB.GestionSolicitudes.Infraestructura.Configuracion;

namespace SB.GestionSolicitudes.Infraestructura.Seguridad;

/// <summary>
/// Issues signed JWTs. Claims include the user id, the role and the display
/// name; authorization decisions are still made server-side from the role and
/// the database, never from client-supplied values.
/// </summary>
public sealed class GeneradorTokenJwt : IGeneradorTokenJwt
{
    private readonly OpcionesJwt _opciones;
    private readonly IReloj _reloj;

    public GeneradorTokenJwt(IOptions<OpcionesJwt> opciones, IReloj reloj)
    {
        _opciones = opciones.Value;
        _reloj = reloj;
    }

    public TokenGenerado Generar(Usuario usuario)
    {
        var expiraEn = _reloj.Ahora.AddMinutes(_opciones.MinutosVigencia);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Email, usuario.Correo),
            new(ClaimTypes.Name, usuario.Nombre),
            new(ClaimTypes.Role, usuario.Rol.ToString())
        };

        var credenciales = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opciones.ClaveSecreta)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _opciones.Emisor,
            audience: _opciones.Audiencia,
            claims: claims,
            notBefore: _reloj.Ahora,
            expires: expiraEn,
            signingCredentials: credenciales);

        return new TokenGenerado(new JwtSecurityTokenHandler().WriteToken(token), expiraEn);
    }
}
