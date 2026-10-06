using System.IO;
using osakaautomate.Dtos;

namespace osakaautomate.Services;

public class LectorPagosCsvService : ILectorPagosCsvService
{
    public List<PagoCsvDto> LeerPagos(string rutaArchivo)
    {
        var pagos = TablaCsv.Leer(rutaArchivo)
            .Where(registro => string.Equals(TablaCsv.Obtener(registro, "Estatus"), "Recibido", StringComparison.OrdinalIgnoreCase))
            .Select(registro => CrearPago(registro))
            .Where(pago => pago.Monto > 0m
                && (pago.Facturas.Count > 0 || (pago.EsNotaCredito && !string.IsNullOrWhiteSpace(pago.CodigoCliente))))
            .ToList();

        if (pagos.Count == 0)
        {
            throw new InvalidDataException("El CSV no contiene pagos recibidos asociados a facturas.");
        }

        return pagos;
    }

    private static PagoCsvDto CrearPago(IReadOnlyDictionary<string, string> registro)
    {
        var comment = TablaCsv.Obtener(registro, "Comentario");
        var columnaFecha = string.IsNullOrWhiteSpace(TablaCsv.Obtener(registro, "Fecha de ingreso"))
            || string.Equals(TablaCsv.Obtener(registro, "Fecha de ingreso"), "null", StringComparison.OrdinalIgnoreCase)
            ? "Fecha de expiración"
            : "Fecha de ingreso";
        var fecha = TablaCsv.ObtenerFecha(registro, columnaFecha);
        var tipo = TablaCsv.Obtener(registro, "Tipo");

        return new PagoCsvDto
        {
            Tipo = tipo,
            CodigoCliente = TablaCsv.Obtener(registro, "Cliente"),
            Monto = Math.Abs(TablaCsv.ObtenerDecimal(registro, "Monto")),
            FechaPago = fecha,
            EsNotaCredito = string.Equals(tipo, "Nota de crédito", StringComparison.OrdinalIgnoreCase),
            Facturas = NormalizadorNumeroFactura.Extraer(comment)
        };
    }
}