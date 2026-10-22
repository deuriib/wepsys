using Shouldly;
using SB.GestionSolicitudes.Dominio.Solicitudes;
using Xunit;

namespace SB.GestionSolicitudes.Pruebas.Unitarias;

/// <summary>
/// The workflow map is the contract shared by the API and the UI, so it deserves
/// its own set of explicit cases.
/// </summary>
public sealed class FlujoEstadoSolicitudPruebas
{
    [Theory]
    [InlineData(EstadoSolicitud.Registrada, EstadoSolicitud.EnAnalisis)]
    [InlineData(EstadoSolicitud.Registrada, EstadoSolicitud.EnProgreso)]
    [InlineData(EstadoSolicitud.EnAnalisis, EstadoSolicitud.Resuelta)]
    [InlineData(EstadoSolicitud.EnProgreso, EstadoSolicitud.EnEsperaDelSolicitante)]
    [InlineData(EstadoSolicitud.EnEsperaDelSolicitante, EstadoSolicitud.EnProgreso)]
    [InlineData(EstadoSolicitud.Resuelta, EstadoSolicitud.Cerrada)]
    [InlineData(EstadoSolicitud.Cerrada, EstadoSolicitud.EnAnalisis)]
    public void TransicionesPermitidas(EstadoSolicitud actual, EstadoSolicitud nuevo)
    {
        FlujoEstadoSolicitud.EsTransicionPermitida(actual, nuevo).ShouldBeTrue();
    }

    [Theory]
    [InlineData(EstadoSolicitud.Registrada, EstadoSolicitud.Cerrada)]
    [InlineData(EstadoSolicitud.Registrada, EstadoSolicitud.Resuelta)]
    [InlineData(EstadoSolicitud.Cerrada, EstadoSolicitud.Resuelta)]
    [InlineData(EstadoSolicitud.Resuelta, EstadoSolicitud.EnAnalisis)]
    [InlineData(EstadoSolicitud.EnAnalisis, EstadoSolicitud.Registrada)]
    public void TransicionesNoPermitidas(EstadoSolicitud actual, EstadoSolicitud nuevo)
    {
        FlujoEstadoSolicitud.EsTransicionPermitida(actual, nuevo).ShouldBeFalse();
    }

    [Fact]
    public void UnEstadoNoEsTransicionValidaHaciaSiMismo()
    {
        foreach (var estado in Enum.GetValues<EstadoSolicitud>())
        {
            FlujoEstadoSolicitud.EsTransicionPermitida(estado, estado).ShouldBeFalse();
        }
    }

    [Fact]
    public void LosDestinosPermitidosSiempreExisten()
    {
        foreach (var estado in Enum.GetValues<EstadoSolicitud>())
        {
            FlujoEstadoSolicitud.ObtenerDestinosPermitidos(estado).ShouldNotBeEmpty();
        }
    }
}
