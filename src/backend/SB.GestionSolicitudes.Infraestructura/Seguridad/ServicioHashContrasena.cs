using System.Security.Cryptography;

namespace SB.GestionSolicitudes.Infraestructura.Seguridad;

using SB.GestionSolicitudes.Aplicacion.Contratos;

/// <summary>
/// PBKDF2-SHA256 password hashing with a per-password random salt.
/// The stored value is <c>iterations.salt.hash</c> so the iteration count can be
/// raised later without invalidating existing credentials.
/// </summary>
public sealed class ServicioHashContrasena : IServicioHashContrasena
{
    private const int ITERACIONES = 120_000;
    private const int LONGITUD_SAL = 16;
    private const int LONGITUD_CLAVE = 32;
    private const char SEPARADOR = '.';

    public string Hashear(string contrasena)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contrasena);

        var sal = RandomNumberGenerator.GetBytes(LONGITUD_SAL);
        var clave = Rfc2898DeriveBytes.Pbkdf2(
            contrasena,
            sal,
            ITERACIONES,
            HashAlgorithmName.SHA256,
            LONGITUD_CLAVE);

        return string.Join(
            SEPARADOR,
            ITERACIONES.ToString(),
            Convert.ToBase64String(sal),
            Convert.ToBase64String(clave));
    }

    public bool Verificar(string contrasena, string hashAlmacenado)
    {
        if (string.IsNullOrWhiteSpace(contrasena) || string.IsNullOrWhiteSpace(hashAlmacenado))
        {
            return false;
        }

        var partes = hashAlmacenado.Split(SEPARADOR);
        if (partes.Length != 3 || !int.TryParse(partes[0], out var iteraciones))
        {
            return false;
        }

        try
        {
            var sal = Convert.FromBase64String(partes[1]);
            var claveEsperada = Convert.FromBase64String(partes[2]);
            var claveCalculada = Rfc2898DeriveBytes.Pbkdf2(
                contrasena,
                sal,
                iteraciones,
                HashAlgorithmName.SHA256,
                claveEsperada.Length);

            return CryptographicOperations.FixedTimeEquals(claveCalculada, claveEsperada);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
