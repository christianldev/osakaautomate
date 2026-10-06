using osakaautomate.Dtos;
namespace osakaautomate.Services;

public interface IMotorCalculoService
{
    IEnumerable<ComisionReporteDto> CalcularComisiones(
        List<FacturaCsvDto> facturas,
        List<PagoCsvDto> pagos);
}