using System.Text.RegularExpressions;
using System.IO;
using ClosedXML.Excel;

namespace osakaautomate.Services;

public partial class LectorPagosExcelService : ILectorPagosExcelService
{
    public List<string> LeerFacturasPagadas(string rutaArchivo)
    {
        if (string.IsNullOrWhiteSpace(rutaArchivo))
        {
            throw new ArgumentException("La ruta del archivo es obligatoria.", nameof(rutaArchivo));
        }

        if (!File.Exists(rutaArchivo))
        {
            throw new FileNotFoundException("No se encontró el archivo de pagos.", rutaArchivo);
        }

        using var workbook = new XLWorkbook(rutaArchivo);
        var worksheet = workbook.Worksheets.FirstOrDefault()
            ?? throw new InvalidOperationException("El archivo de pagos no contiene hojas.");
        var headerRow = worksheet.FirstRowUsed()
            ?? throw new InvalidOperationException("El archivo de pagos no contiene datos.");
        var descripcionCell = headerRow.CellsUsed()
            .FirstOrDefault(cell => EsEncabezadoDescripcion(cell.GetString()));

        if (descripcionCell is null)
        {
            throw new InvalidOperationException("No se encontró la columna 'Descripción'.");
        }

        var facturas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var cell in worksheet.Column(descripcionCell.Address.ColumnNumber)
                     .CellsUsed()
                     .Where(cell => cell.Address.RowNumber > headerRow.RowNumber()))
        {
            foreach (Match match in NumeroFacturaRegex().Matches(cell.GetString()))
            {
                facturas.Add(match.Value.ToUpperInvariant());
            }
        }

        return facturas.OrderBy(numeroFactura => numeroFactura).ToList();
    }

    private static bool EsEncabezadoDescripcion(string encabezado)
    {
        return string.Equals(encabezado.Trim(), "Descripción", StringComparison.OrdinalIgnoreCase)
            || string.Equals(encabezado.Trim(), "Descripcion", StringComparison.OrdinalIgnoreCase);
    }

    [GeneratedRegex(@"\b(?:FAC(?:TURA)?[-\s]?\d+|[A-Z]{2,10}-\d+|\d{4,})\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex NumeroFacturaRegex();
}