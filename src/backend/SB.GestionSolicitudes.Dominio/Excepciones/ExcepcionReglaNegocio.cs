namespace SB.GestionSolicitudes.Dominio.Excepciones;

/// <summary>
/// Represents a violation of a business rule owned by the domain layer.
/// The API layer translates it into an HTTP 409/400 response; the domain never
/// knows about HTTP, which keeps the business rules reusable and testable.
/// </summary>
public class ExcepcionReglaNegocio : Exception
{
    public ExcepcionReglaNegocio(string mensaje)
        : base(mensaje)
    {
    }

    public ExcepcionReglaNegocio(string mensaje, Exception excepcionInterna)
        : base(mensaje, excepcionInterna)
    {
    }
}
