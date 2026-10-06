using osakaautomate.Dtos;

namespace osakaautomate.Services;

public class MotorCalculoService : IMotorCalculoService
{
    private const decimal PorcentajeComisionProvisional = 0.03m;

    public IEnumerable<ComisionReporteDto> CalcularComisiones(
        List<FacturaCsvDto> facturas,
        List<PagoCsvDto> pagos)
    {
        ArgumentNullException.ThrowIfNull(facturas);
        ArgumentNullException.ThrowIfNull(pagos);

        var facturasPorNumero = facturas
            .Where(factura => !string.IsNullOrWhiteSpace(factura.NumeroFactura))
            .ToDictionary(factura => factura.NumeroFactura, StringComparer.OrdinalIgnoreCase);
        var acumulados = facturasPorNumero.Keys.ToDictionary(
            numero => numero,
            _ => new AcumuladoPago(),
            StringComparer.OrdinalIgnoreCase);

        foreach (var pago in pagos.Where(pago => !pago.EsNotaCredito))
        {
            var facturasRelacionadas = pago.Facturas
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(facturasPorNumero.ContainsKey)
                .Select(numero => facturasPorNumero[numero])
                .ToList();

            if (facturasRelacionadas.Count == 0)
            {
                continue;
            }

            DistribuirPago(pago, facturasRelacionadas, acumulados);
        }

        AplicarNotasCredito(pagos.Where(pago => pago.EsNotaCredito), facturasPorNumero, acumulados);

        return facturasPorNumero.Values
            .Where(factura => acumulados[factura.NumeroFactura].TotalRecibido > 0m)
            .Select(factura => CrearResultado(factura, acumulados[factura.NumeroFactura]))
            .OrderBy(resultado => resultado.FechaPago)
            .ThenBy(resultado => resultado.Vendedor, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(resultado => resultado.NumeroFactura, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void AplicarNotasCredito(
        IEnumerable<PagoCsvDto> notasCredito,
        IReadOnlyDictionary<string, FacturaCsvDto> facturas,
        IReadOnlyDictionary<string, AcumuladoPago> acumulados)
    {
        foreach (var nota in notasCredito.OrderBy(nota => nota.FechaPago))
        {
            var montoPendiente = nota.Monto;
            var facturasDelCliente = facturas.Values
                .Where(factura => string.Equals(factura.CodigoCliente, nota.CodigoCliente, StringComparison.OrdinalIgnoreCase)
                    && acumulados[factura.NumeroFactura].TotalRecibido > 0m)
                .OrderBy(factura => factura.FechaFactura)
                .ToList();

            foreach (var factura in facturasDelCliente)
            {
                var acumulado = acumulados[factura.NumeroFactura];
                var totalFactura = Math.Max(factura.Total, factura.Subtotal + factura.Iva);
                var saldo = Math.Max(0m, totalFactura - acumulado.TotalRecibido);
                var montoAplicado = Math.Min(montoPendiente, saldo);

                if (montoAplicado <= 0m)
                {
                    continue;
                }

                acumulado.NotasCredito += montoAplicado;
                montoPendiente -= montoAplicado;
                if (nota.FechaPago > acumulado.FechaPago)
                {
                    acumulado.FechaPago = nota.FechaPago;
                }

                if (montoPendiente <= 0m)
                {
                    break;
                }
            }
        }
    }

    private static void DistribuirPago(
        PagoCsvDto pago,
        IReadOnlyList<FacturaCsvDto> facturasRelacionadas,
        IReadOnlyDictionary<string, AcumuladoPago> acumulados)
    {
        var montosBase = facturasRelacionadas
            .Select(factura => Math.Max(factura.Total, factura.Subtotal + factura.Iva))
            .ToList();
        var totalBase = montosBase.Sum();
        var montoPendiente = pago.Monto;
        var esRetencion = pago.Tipo.Contains("retencion", StringComparison.OrdinalIgnoreCase)
            || pago.Tipo.Contains("retención", StringComparison.OrdinalIgnoreCase);

        for (var index = 0; index < facturasRelacionadas.Count; index++)
        {
            var montoAsignado = index == facturasRelacionadas.Count - 1
                ? montoPendiente
                : totalBase > 0m
                    ? Math.Round(pago.Monto * montosBase[index] / totalBase, 2, MidpointRounding.AwayFromZero)
                    : Math.Round(pago.Monto / facturasRelacionadas.Count, 2, MidpointRounding.AwayFromZero);

            montoPendiente -= montoAsignado;
            var acumulado = acumulados[facturasRelacionadas[index].NumeroFactura];
            if (esRetencion)
            {
                acumulado.Retenciones += montoAsignado;
            }
            else
            {
                acumulado.TotalCobrado += montoAsignado;
            }

            if (pago.FechaPago > acumulado.FechaPago)
            {
                acumulado.FechaPago = pago.FechaPago;
            }
        }
    }

    private static ComisionReporteDto CrearResultado(FacturaCsvDto factura, AcumuladoPago pago)
    {
        var totalFactura = Math.Max(factura.Total, factura.Subtotal + factura.Iva);
        var totalRecibido = Math.Min(pago.TotalRecibido, totalFactura);
        var cobroNeto = totalFactura > 0m
            ? Math.Min(factura.Subtotal, Math.Max(0m, totalRecibido * factura.Subtotal / totalFactura))
            : 0m;
        var dias = Math.Max(0, (pago.FechaPago.Date - factura.FechaFactura.Date).Days);

        return new ComisionReporteDto
        {
            Cliente = factura.Cliente,
            Sucursal = factura.Sucursal,
            NumeroFactura = factura.NumeroFactura,
            Vendedor = factura.Vendedor,
            FechaFactura = factura.FechaFactura,
            VentaBruta = factura.Subtotal + factura.Descuento,
            Descuento = factura.Descuento,
            TotalCobrado = pago.TotalCobrado,
            Iva = factura.Iva,
            FechaPago = pago.FechaPago,
            CobroNeto = cobroNeto,
            Dias = dias,
            PorcentajeComision = PorcentajeComisionProvisional,
            TotalComision = Math.Round(cobroNeto * PorcentajeComisionProvisional, 2, MidpointRounding.AwayFromZero),
            Retenciones = pago.Retenciones
        };
    }

    private sealed class AcumuladoPago
    {
        public decimal TotalCobrado { get; set; }
        public decimal Retenciones { get; set; }
        public decimal NotasCredito { get; set; }
        public DateTime FechaPago { get; set; }
        public decimal TotalRecibido => TotalCobrado + Retenciones + NotasCredito;
    }
}