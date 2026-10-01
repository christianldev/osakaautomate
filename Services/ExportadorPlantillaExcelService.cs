using System.IO;
using ClosedXML.Excel;
using osakaautomate.Models;

namespace osakaautomate.Services;

public class ExportadorPlantillaExcelService : IExportadorPlantillaExcelService
{
    private const int FilaInicial = 5;
    private const int ColumnaFactura = 1;
    private const int ColumnaVendedor = 2;
    private const int ColumnaComision = 3;

    public string ExportarComisiones(
        IEnumerable<FacturaProcesada> comisiones,
        string rutaPlantilla)
    {
        ArgumentNullException.ThrowIfNull(comisiones);

        if (string.IsNullOrWhiteSpace(rutaPlantilla))
        {
            throw new ArgumentException("La ruta de la plantilla es obligatoria.", nameof(rutaPlantilla));
        }

        if (!File.Exists(rutaPlantilla))
        {
            throw new FileNotFoundException("No se encontró la plantilla de Excel.", rutaPlantilla);
        }

        using var workbook = new XLWorkbook(rutaPlantilla);
        var worksheet = workbook.Worksheets.FirstOrDefault()
            ?? throw new InvalidOperationException("La plantilla no contiene hojas de cálculo.");
        var filaActual = FilaInicial;

        foreach (var factura in comisiones)
        {
            worksheet.Cell(filaActual, ColumnaFactura).Value = factura.NumeroFactura;
            worksheet.Cell(filaActual, ColumnaVendedor).Value = factura.Vendedor?.Nombre ?? string.Empty;
            worksheet.Cell(filaActual, ColumnaComision).Value = factura.MontoComision;
            worksheet.Cell(filaActual, ColumnaComision).Style.NumberFormat.Format = "$#,##0.00";
            filaActual++;
        }

        var rutaSalida = ObtenerRutaSalida(rutaPlantilla);
        workbook.SaveAs(rutaSalida);

        return rutaSalida;
    }

    private static string ObtenerRutaSalida(string rutaPlantilla)
    {
        var directorio = Path.GetDirectoryName(rutaPlantilla)
            ?? throw new InvalidOperationException("No se pudo determinar el directorio de la plantilla.");
        var rutaSalida = Path.Combine(directorio, "Comisiones_Generadas.xlsx");
        var indice = 1;

        while (File.Exists(rutaSalida))
        {
            rutaSalida = Path.Combine(directorio, $"Comisiones_Generadas_{indice}.xlsx");
            indice++;
        }

        return rutaSalida;
    }
}