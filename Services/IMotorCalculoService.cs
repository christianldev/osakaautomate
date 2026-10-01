using osakaautomate.Dtos;
using osakaautomate.Models;

namespace osakaautomate.Services;

public interface IMotorCalculoService
{
    IEnumerable<FacturaProcesada> CalcularComisiones(
        List<string> facturasPagadas,
        List<FacturaDto> facturasLeidas);
}