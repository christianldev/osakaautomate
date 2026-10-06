using System.IO;
using System.Globalization;
using ClosedXML.Excel;
using osakaautomate.Dtos;

namespace osakaautomate.Services;

public class ExportadorPlantillaExcelService : IExportadorPlantillaExcelService
{
    private static readonly string[] Encabezados =
    [
        "CLIENTE", "CIUDAD", "FACTURA", "F/FACTURA", "VTA. BRUTA", "DSCTO.",
        "COBRADO", "IVA", "FECHA DE PAGO", "Neto", "Dias", "Com.", "Comisión"
    ];

    public string ExportarComisiones(
        IEnumerable<ComisionReporteDto> comisiones,
        string rutaHistorico,
        string rutaArchivo)
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

        if (string.IsNullOrWhiteSpace(rutaHistorico) || !File.Exists(rutaHistorico))
        {
            throw new FileNotFoundException("Carga el libro histórico de comisiones antes de exportar.", rutaHistorico);
        }

        if (string.Equals(Path.GetFullPath(rutaHistorico), Path.GetFullPath(rutaArchivo), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Guarda el reporte en una copia distinta del libro histórico.");
        }

        using var workbook = new XLWorkbook(rutaHistorico);
        workbook.CalculateMode = XLCalculateMode.Auto;
        // ClosedXML 0.105 no puede serializar algunos autofiltros heredados
        // del libro histórico (especialmente rangos de una sola columna).
        // Se conservan los datos y estilos, pero se limpian los filtros antes
        // de guardar la copia para evitar un XLSX corrupto o una excepción.
        foreach (var hojaHistorica in workbook.Worksheets)
        {
            if (hojaHistorica.AutoFilter.IsEnabled)
            {
                hojaHistorica.AutoFilter.Clear();
            }
        }
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
        var ultimaColumna = Encabezados.Length;
        const int filaEncabezado = 7;
        var worksheetExistente = EncontrarHojaDestino(workbook, vendedor, anio, mes);
        var worksheet = worksheetExistente ?? workbook.Worksheets.Add(
            ObtenerNombreHojaNueva(workbook, vendedor, anio, mes));
        var usaPlantillaHistorica = worksheetExistente is not null;

        var ultimaFilaAnterior = worksheet.RangeUsed()?.RangeAddress.LastAddress.RowNumber ?? 7;
        var ultimaFilaNecesaria = Math.Max(ultimaFilaAnterior, filaEncabezado + resultados.Count + 1);
        worksheet.Range(8, 1, ultimaFilaNecesaria, ultimaColumna)
            .Clear(XLClearOptions.Contents);

        if (!usaPlantillaHistorica)
        {
            worksheet.Range(3, 1, 3, ultimaColumna).Merge();
        }
        worksheet.Cell(3, 1).Value = "RECUPERACION DE CARTERA /PAGO COMISIONES";
        if (!usaPlantillaHistorica)
        {
            worksheet.Range(3, 1, 3, ultimaColumna).Style.Font.Bold = true;
            worksheet.Range(3, 1, 3, ultimaColumna).Style.Font.FontSize = 14;
            worksheet.Range(3, 1, 3, ultimaColumna).Style.Fill.BackgroundColor = XLColor.FromHtml("#E3B85B");
        }

        worksheet.Cell(4, 2).Value = $"Mes : {ObtenerAbreviaturaMes(mes)}/{anio}";
        worksheet.Cell(5, 2).Value = $"Vendedor : {vendedor}";
        if (!usaPlantillaHistorica)
        {
            worksheet.Cell(5, 2).Style.Font.Bold = true;
        }
        worksheet.Cell(6, 7).Value = "TOTAL";
        worksheet.Cell(6, 10).Value = "Cobro";
        worksheet.Cell(6, 11).Value = "Dias";
        worksheet.Cell(6, 12).Value = "%";
        worksheet.Cell(6, 13).Value = "Total";

        for (var index = 0; index < Encabezados.Length; index++)
        {
            worksheet.Cell(filaEncabezado, index + 1).Value = Encabezados[index];
        }

        var encabezado = worksheet.Range(filaEncabezado, 1, filaEncabezado, ultimaColumna);
        if (!usaPlantillaHistorica)
        {
            encabezado.Style.Font.Bold = true;
            encabezado.Style.Font.FontColor = XLColor.White;
            encabezado.Style.Fill.BackgroundColor = XLColor.FromHtml("#24534E");
            encabezado.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            encabezado.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            encabezado.Style.Alignment.WrapText = true;
            encabezado.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
        }

        var fila = filaEncabezado + 1;
        foreach (var resultado in resultados.OrderBy(resultado => resultado.FechaPago).ThenBy(resultado => resultado.NumeroFactura))
        {
            worksheet.Cell(fila, 1).Value = resultado.Cliente;
            worksheet.Cell(fila, 2).Value = resultado.Sucursal;
            worksheet.Cell(fila, 3).Value = resultado.NumeroFactura;
            worksheet.Cell(fila, 4).Value = resultado.FechaFactura.Date;
            worksheet.Cell(fila, 5).FormulaA1 = $"={resultado.Subtotal.ToString("0.################", CultureInfo.InvariantCulture)}+F{fila}";
            worksheet.Cell(fila, 6).Value = resultado.Descuento;
            worksheet.Cell(fila, 7).Value = resultado.TotalCobrado;
            worksheet.Cell(fila, 8).Value = resultado.Iva;
            worksheet.Cell(fila, 9).Value = resultado.FechaPago;
            worksheet.Cell(fila, 10).FormulaA1 = $"=E{fila}-F{fila}";
            worksheet.Cell(fila, 11).FormulaA1 = $"=ROUND(I{fila}-D{fila},0)";
            worksheet.Cell(fila, 12).Value = resultado.PorcentajeComision;
            worksheet.Cell(fila, 13).FormulaA1 = $"=J{fila}*L{fila}";
            fila++;
        }

        var primeraFilaDatos = filaEncabezado + 1;
        var ultimaFilaDatos = fila - 1;
        worksheet.Range(primeraFilaDatos, 4, ultimaFilaDatos, 4).Style.DateFormat.Format = "dd/mm/yy";
        worksheet.Range(primeraFilaDatos, 9, ultimaFilaDatos, 9).Style.DateFormat.Format = "dd/mm/yy";
        foreach (var columnaMonetaria in new[] { 5, 6, 7, 8, 10, 13 })
        {
            worksheet.Range(primeraFilaDatos, columnaMonetaria, ultimaFilaDatos, columnaMonetaria)
                .Style.NumberFormat.Format = "$#,##0.00";
        }

        worksheet.Range(primeraFilaDatos, 12, ultimaFilaDatos, 12).Style.NumberFormat.Format = "0.0%";
        worksheet.Range(primeraFilaDatos, 11, ultimaFilaDatos, 11).Style.NumberFormat.Format = "0";
        if (!usaPlantillaHistorica)
        {
            worksheet.Range(primeraFilaDatos, 1, ultimaFilaDatos, ultimaColumna).Style.Border.BottomBorder = XLBorderStyleValues.Hair;
        }

        var filaTotal = ultimaFilaDatos + 1;
        worksheet.Cell(filaTotal, 1).Value = "TOTALES";
        foreach (var columna in new[] { 5, 6, 7, 8, 10, 13 })
        {
            var letraColumna = XLHelper.GetColumnLetterFromNumber(columna);
            worksheet.Cell(filaTotal, columna).FormulaA1 = $"=SUM({letraColumna}{primeraFilaDatos}:{letraColumna}{ultimaFilaDatos})";
        }

        if (!usaPlantillaHistorica)
        {
            worksheet.Range(filaTotal, 1, filaTotal, ultimaColumna).Style.Font.Bold = true;
            worksheet.Range(filaTotal, 1, filaTotal, ultimaColumna).Style.Border.TopBorder = XLBorderStyleValues.Thin;
        }
        worksheet.SheetView.FreezeRows(filaEncabezado);
        worksheet.Range(filaEncabezado, 1, ultimaFilaDatos, ultimaColumna).SetAutoFilter();
        if (!usaPlantillaHistorica)
        {
            worksheet.Columns().AdjustToContents(8, 30);
            worksheet.Column(1).Width = 34;
            worksheet.Column(2).Width = 18;
            worksheet.Column(3).Width = 22;
            worksheet.Row(filaEncabezado).Height = 32;
        }
    }

    private static IXLWorksheet? EncontrarHojaDestino(XLWorkbook workbook, string vendedor, int anio, int mes)
    {
        var nombreBase = $"COM.{ObtenerAbreviaturaMes(mes)}.{anio}";
        var vendedorNormalizado = TablaCsv.NormalizarNombreCliente(vendedor);
        return workbook.Worksheets.FirstOrDefault(hoja =>
            hoja.Name.StartsWith(nombreBase, StringComparison.OrdinalIgnoreCase)
            && TablaCsv.NormalizarNombreCliente(ObtenerVendedorHoja(hoja)) == vendedorNormalizado);
    }

    private static string ObtenerNombreHojaNueva(XLWorkbook workbook, string vendedor, int anio, int mes)
    {
        var nombreBase = $"COM.{ObtenerAbreviaturaMes(mes)}.{anio}";
        if (!workbook.Worksheets.Any(hoja => string.Equals(hoja.Name, nombreBase, StringComparison.OrdinalIgnoreCase)))
        {
            return nombreBase;
        }

        var nombreVendedor = new string(vendedor
            .Where(caracter => char.IsLetterOrDigit(caracter) || caracter == ' ')
            .ToArray())
            .Trim();
        var nombre = $"{nombreBase} {nombreVendedor}";
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

    private static string ObtenerVendedorHoja(IXLWorksheet worksheet)
    {
        var rango = worksheet.RangeUsed();
        if (rango is null)
        {
            return string.Empty;
        }

        foreach (var celda in rango.CellsUsed().Where(celda => celda.Address.RowNumber <= 6))
        {
            var texto = celda.GetString().Trim();
            if (texto.StartsWith("Vendedor", StringComparison.OrdinalIgnoreCase))
            {
                var separador = texto.IndexOf(':');
                if (separador >= 0)
                {
                    return texto[(separador + 1)..].Trim();
                }
            }
        }

        return string.Empty;
    }

    private static string ObtenerAbreviaturaMes(int mes)
    {
        return mes switch
        {
            1 => "ENE",
            2 => "FEB",
            3 => "MAR",
            4 => "ABR",
            5 => "MAY",
            6 => "JUN",
            7 => "JUL",
            8 => "AGO",
            9 => "SEPT",
            10 => "OCT",
            11 => "NOV",
            12 => "DIC",
            _ => throw new ArgumentOutOfRangeException(nameof(mes))
        };
    }
}
