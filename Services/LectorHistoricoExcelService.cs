using System.IO;
using ClosedXML.Excel;

namespace osakaautomate.Services;

public class LectorHistoricoExcelService : ILectorHistoricoExcelService
{
    public Dictionary<string, string> LeerCiudadesPorCliente(string rutaArchivo)
    {
        if (string.IsNullOrWhiteSpace(rutaArchivo))
        {
            throw new ArgumentException("La ruta del histórico es obligatoria.", nameof(rutaArchivo));
        }

        if (!File.Exists(rutaArchivo))
        {
            throw new FileNotFoundException("No se encontró el libro histórico de comisiones.", rutaArchivo);
        }

        using var workbook = new XLWorkbook(rutaArchivo);
        var ciudadesPorCliente = new Dictionary<string, Dictionary<string, int>>(StringComparer.OrdinalIgnoreCase);

        foreach (var worksheet in workbook.Worksheets)
        {
            var filasUsadas = worksheet.RowsUsed().ToList();
            var encabezado = filasUsadas.FirstOrDefault(fila =>
                fila.CellsUsed().Any(celda => EsEncabezado(celda.GetString(), "CLIENTE"))
                && fila.CellsUsed().Any(celda => EsEncabezado(celda.GetString(), "CIUDAD")));

            if (encabezado is null)
            {
                continue;
            }

            var columnaCliente = encabezado.CellsUsed()
                .First(celda => EsEncabezado(celda.GetString(), "CLIENTE"))
                .Address.ColumnNumber;
            var columnaCiudad = encabezado.CellsUsed()
                .First(celda => EsEncabezado(celda.GetString(), "CIUDAD"))
                .Address.ColumnNumber;
            var columnaFactura = encabezado.CellsUsed()
                .FirstOrDefault(celda => EsEncabezado(celda.GetString(), "FACTURA"))?
                .Address.ColumnNumber ?? 0;

            foreach (var fila in filasUsadas.Where(fila => fila.RowNumber() > encabezado.RowNumber()))
            {
                var cliente = fila.Cell(columnaCliente).GetString().Trim();
                var ciudad = fila.Cell(columnaCiudad).GetString().Trim();
                if (string.IsNullOrWhiteSpace(cliente) || string.IsNullOrWhiteSpace(ciudad)
                    || (columnaFactura > 0 && NormalizadorNumeroFactura.Normalizar(fila.Cell(columnaFactura).GetString()) is null))
                {
                    continue;
                }

                var claveCliente = TablaCsv.NormalizarNombreCliente(cliente);
                if (!ciudadesPorCliente.TryGetValue(claveCliente, out var frecuencias))
                {
                    frecuencias = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                    ciudadesPorCliente.Add(claveCliente, frecuencias);
                }

                frecuencias[ciudad] = frecuencias.GetValueOrDefault(ciudad) + 1;
            }
        }

        return ciudadesPorCliente.ToDictionary(
            cliente => cliente.Key,
            cliente => cliente.Value.OrderByDescending(ciudad => ciudad.Value).First().Key,
            StringComparer.OrdinalIgnoreCase);
    }

    private static bool EsEncabezado(string valor, string esperado)
    {
        return string.Equals(valor.Trim(), esperado, StringComparison.OrdinalIgnoreCase);
    }
}