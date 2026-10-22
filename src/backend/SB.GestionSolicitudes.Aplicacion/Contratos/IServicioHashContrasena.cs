namespace SB.GestionSolicitudes.Aplicacion.Contratos;

/// <summary>
/// Password hashing contract (PBKDF2 in infrastructure). Hashing never leaks
/// into the domain, and the algorithm can change without touching business rules.
/// </summary>
public interface IServicioHashContrasena
{
    string Hashear(string contrasena);

    bool Verificar(string contrasena, string hashAlmacenado);
}
