using System.IO;
using osakaautomate.Dtos;

namespace osakaautomate.Services;

public class LectorFacturasCsvService : ILectorFacturasCsvService
{
    public List<FacturaCsvDto> LeerFacturas(string rutaArchivo)
    {
        var facturasValidas = TablaCsv.Leer(rutaArchivo)
            .Where(registro => string.Equals(TablaCsv.Obtener(registro, "Tipo"), "Factura", StringComparison.OrdinalIgnoreCase))
            .Where(registro => !string.Equals(TablaCsv.Obtener(registro, "Estatus"), "Anulado", StringComparison.OrdinalIgnoreCase))
            .Select(registro => CrearFactura(registro))
            .Where(factura => !string.IsNullOrWhiteSpace(factura.NumeroFactura))
            .GroupBy(factura => factura.NumeroFactura, StringComparer.OrdinalIgnoreCase)
            .Select(grupo => grupo.OrderByDescending(factura => factura.FechaFactura).First())
            .ToList();

        if (facturasValidas.Count == 0)
        {
            throw new InvalidDataException("El CSV no contiene facturas válidas.");
        }

        return facturasValidas;
    }

    private static FacturaCsvDto CrearFactura(IReadOnlyDictionary<string, string> registro)
    {
        var numeroFactura = NormalizadorNumeroFactura.Normalizar(TablaCsv.Obtener(registro, "Código"));
        if (numeroFactura is null)
        {
            return new FacturaCsvDto();
        }

        return new FacturaCsvDto
        {
            NumeroFactura = numeroFactura,
            CodigoCliente = TablaCsv.Obtener(registro, "Cliente"),
            Cliente = TablaCsv.Obtener(registro, "Cliente (Nombre)"),
            Sucursal = TablaCsv.Obtener(registro, "Dirección de entrega"),
            Vendedor = TablaCsv.Obtener(registro, "Usuario (Nombre)"),
            CodigoVendedor = TablaCsv.Obtener(registro, "Usuario"),
            FechaFactura = TablaCsv.ObtenerFecha(registro, "Fecha de creación"),
            Subtotal = TablaCsv.ObtenerDecimal(registro, "Subtotal"),
            Descuento = TablaCsv.ObtenerDecimal(registro, "Descuento"),
            Iva = TablaCsv.ObtenerDecimal(registro, "Impuestos"),
            Total = TablaCsv.ObtenerDecimal(registro, "Total")
        };
    }
}