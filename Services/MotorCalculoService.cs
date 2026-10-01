using osakaautomate.Data;
using osakaautomate.Dtos;
using osakaautomate.Models;

namespace osakaautomate.Services;

public class MotorCalculoService : IMotorCalculoService
{
    private readonly AppDbContext _context;

    public MotorCalculoService(AppDbContext context)
    {
        _context = context;
    }

    public IEnumerable<FacturaProcesada> CalcularComisiones(
        List<string> facturasPagadas,
        List<FacturaDto> facturasLeidas)
    {
        ArgumentNullException.ThrowIfNull(facturasPagadas);
        ArgumentNullException.ThrowIfNull(facturasLeidas);

        var numerosPagados = new HashSet<string>(
            facturasPagadas
                .Where(numero => !string.IsNullOrWhiteSpace(numero))
                .Select(numero => numero.Trim()),
            StringComparer.OrdinalIgnoreCase);
        var vendedores = _context.Vendedores.ToList();
        var reglas = _context.ReglasComision.ToList();
        var resultados = new List<FacturaProcesada>();

        foreach (var factura in facturasLeidas.Where(factura =>
                     !string.IsNullOrWhiteSpace(factura.NumeroFactura)
                     && numerosPagados.Contains(factura.NumeroFactura.Trim())))
        {
            var vendedor = vendedores.FirstOrDefault(vendedor =>
                string.Equals(vendedor.Nombre, factura.Vendedor.Trim(), StringComparison.OrdinalIgnoreCase));

            if (vendedor is null)
            {
                continue;
            }

            var regla = reglas.FirstOrDefault(regla =>
                string.Equals(regla.NivelVendedor, vendedor.Nivel, StringComparison.OrdinalIgnoreCase)
                && factura.Subtotal >= regla.RangoVentaMinimo
                && factura.Subtotal <= regla.RangoVentaMaximo);

            if (regla is null)
            {
                continue;
            }

            resultados.Add(new FacturaProcesada
            {
                NumeroFactura = factura.NumeroFactura.Trim(),
                VendedorId = vendedor.Id,
                Vendedor = vendedor,
                Subtotal = factura.Subtotal,
                MontoComision = Math.Round(
                    factura.Subtotal * regla.PorcentajeComision / 100m,
                    2,
                    MidpointRounding.AwayFromZero),
                Estado = "Pagada"
            });
        }

        return resultados;
    }
}