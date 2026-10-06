using System.IO;
using System.Globalization;
using ClosedXML.Excel;
using osakaautomate.Dtos;

namespace osakaautomate.Services;

public class ExportadorPlantillaExcelService : IExportadorPlantillaExcelService
{
    private static readonly string[] Encabezados =
    [
        "CLIENTE", "SUCURSAL", "FACTURA", "F/FACTURA", "VTA. BRUTA", "DSCTO.",
        "TOTAL COBRADO", "IVA", "FECHA DE PAGO", "COBRO NETO", "DIAS", "% COM.", "TOTAL COMISIÓN"
    ];

    public string ExportarComisiones(IEnumerable<ComisionReporteDto> comisiones, string rutaArchivo)
    {
        ArgumentNullException.ThrowIfNull(comisiones);
        var resultados = comisiones.ToList();
        if (resultados.Count == 0)
        {
            throw new InvalidOperationException("No hay comisiones calculadas para exportar.");
        }

        if (string.IsNullOrWhiteSpace(rutaArchivo))
        {
            throw new ArgumentException("La ruta de salida es obligatoria.", nameof(rutaArchivo));
        }

        using var workbook = new XLWorkbook();
        foreach (var grupo in resultados
                     .GroupBy(resultado => new
                     {
                         resultado.Vendedor,
                         Anio = resultado.FechaPago.Year,
                         Mes = resultado.FechaPago.Month
                     })
                     .OrderBy(grupo => grupo.Key.Anio)
                     .ThenBy(grupo => grupo.Key.Mes)
                     .ThenBy(grupo => grupo.Key.Vendedor, StringComparer.CurrentCultureIgnoreCase))
        {
            CrearHoja(workbook, grupo.ToList(), grupo.Key.Vendedor, grupo.Key.Anio, grupo.Key.Mes);
        }

        workbook.SaveAs(rutaArchivo);
        return rutaArchivo;
    }

    private static void CrearHoja(
        XLWorkbook workbook,
        IReadOnlyList<ComisionReporteDto> resultados,
        string vendedor,
        int anio,
        int mes)
    {
        var nombreHoja = CrearNombreHoja(workbook, vendedor, anio, mes);
        var worksheet = workbook.Worksheets.Add(nombreHoja);
        var ultimaColumna = Encabezados.Length;

        worksheet.Range(1, 1, 1, ultimaColumna).Merge();
        worksheet.Cell(1, 1).Value = "RECUPERACIÓN DE CARTERA / PAGO COMISIONES";
        worksheet.Range(1, 1, 1, ultimaColumna).Style.Font.Bold = true;
        worksheet.Range(1, 1, 1, ultimaColumna).Style.Font.FontSize = 14;
        worksheet.Range(1, 1, 1, ultimaColumna).Style.Fill.BackgroundColor = XLColor.FromHtml("#E3B85B");

        worksheet.Range(2, 1, 2, ultimaColumna).Merge();
        worksheet.Cell(2, 1).Value = $"Mes: {CultureInfo.GetCultureInfo("es-EC").DateTimeFormat.GetMonthName(mes).ToUpperInvariant()}/{anio}";
        worksheet.Cell(3, 1).Value = $"Vendedor: {vendedor}";
        worksheet.Cell(3, 1).Style.Font.Bold = true;

        const int filaEncabezado = 5;
        for (var index = 0; index < Encabezados.Length; index++)
        {
            worksheet.Cell(filaEncabezado, index + 1).Value = Encabezados[index];
        }

        var encabezado = worksheet.Range(filaEncabezado, 1, filaEncabezado, ultimaColumna);
        encabezado.Style.Font.Bold = true;
        encabezado.Style.Font.FontColor = XLColor.White;
        encabezado.Style.Fill.BackgroundColor = XLColor.FromHtml("#24534E");
        encabezado.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        encabezado.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        encabezado.Style.Alignment.WrapText = true;
        encabezado.Style.Border.BottomBorder = XLBorderStyleValues.Thin;

        var fila = filaEncabezado + 1;
        foreach (var resultado in resultados.OrderBy(resultado => resultado.FechaPago).ThenBy(resultado => resultado.NumeroFactura))
        {
            worksheet.Cell(fila, 1).Value = resultado.Cliente;
            worksheet.Cell(fila, 2).Value = resultado.Sucursal;
            worksheet.Cell(fila, 3).Value = resultado.NumeroFactura;
            worksheet.Cell(fila, 4).Value = resultado.FechaFactura;
            worksheet.Cell(fila, 5).Value = resultado.VentaBruta;
            worksheet.Cell(fila, 6).Value = resultado.Descuento;
            worksheet.Cell(fila, 7).Value = resultado.TotalCobrado;
            worksheet.Cell(fila, 8).Value = resultado.Iva;
            worksheet.Cell(fila, 9).Value = resultado.FechaPago;
            worksheet.Cell(fila, 10).Value = resultado.CobroNeto;
            worksheet.Cell(fila, 11).Value = resultado.Dias;
            worksheet.Cell(fila, 12).Value = resultado.PorcentajeComision;
            worksheet.Cell(fila, 13).Value = resultado.TotalComision;
            fila++;
        }

        var primeraFilaDatos = filaEncabezado + 1;
        var ultimaFilaDatos = fila - 1;
        worksheet.Range(primeraFilaDatos, 4, ultimaFilaDatos, 4).Style.DateFormat.Format = "dd/mm/yy";
        worksheet.Range(primeraFilaDatos, 9, ultimaFilaDatos, 9).Style.DateFormat.Format = "dd/mm/yy";
        worksheet.Range(primeraFilaDatos, 5, ultimaFilaDatos, 10).Style.NumberFormat.Format = "$#,##0.00";
        worksheet.Range(primeraFilaDatos, 12, ultimaFilaDatos, 12).Style.NumberFormat.Format = "0.0%";
        worksheet.Range(primeraFilaDatos, 13, ultimaFilaDatos, 13).Style.NumberFormat.Format = "$#,##0.00";
        worksheet.Range(primeraFilaDatos, 1, ultimaFilaDatos, ultimaColumna).Style.Border.BottomBorder = XLBorderStyleValues.Hair;

        var filaTotal = ultimaFilaDatos + 1;
        worksheet.Cell(filaTotal, 1).Value = "TOTALES";
        foreach (var columna in new[] { 5, 6, 7, 8, 10, 13 })
        {
            worksheet.Cell(filaTotal, columna).Value = resultados.Sum(resultado => columna switch
            {
                5 => resultado.VentaBruta,
                6 => resultado.Descuento,
                7 => resultado.TotalCobrado,
                8 => resultado.Iva,
                10 => resultado.CobroNeto,
                _ => resultado.TotalComision
            });
        }

        worksheet.Range(filaTotal, 1, filaTotal, ultimaColumna).Style.Font.Bold = true;
        worksheet.Range(filaTotal, 1, filaTotal, ultimaColumna).Style.Border.TopBorder = XLBorderStyleValues.Thin;
        worksheet.SheetView.FreezeRows(filaEncabezado);
        worksheet.Range(filaEncabezado, 1, ultimaFilaDatos, ultimaColumna).SetAutoFilter();
        worksheet.Columns().AdjustToContents(8, 30);
        worksheet.Column(1).Width = 34;
        worksheet.Column(2).Width = 18;
        worksheet.Column(3).Width = 22;
        worksheet.Row(filaEncabezado).Height = 32;
    }

    private static string CrearNombreHoja(XLWorkbook workbook, string vendedor, int anio, int mes)
    {
        var nombre = $"{vendedor} {anio}-{mes:00}";
        foreach (var caracter in new[] { ':', '\\', '/', '?', '*', '[', ']' })
        {
            nombre = nombre.Replace(caracter, ' ');
        }

        nombre = nombre.Trim();
        if (nombre.Length > 31)
        {
            nombre = nombre[..31];
        }

        var baseNombre = nombre;
        var indice = 1;
        while (workbook.Worksheets.Any(hoja => string.Equals(hoja.Name, nombre, StringComparison.OrdinalIgnoreCase)))
        {
            var sufijo = $"_{indice++}";
            nombre = baseNombre[..Math.Min(baseNombre.Length, 31 - sufijo.Length)] + sufijo;
        }

        return nombre;
    }
}