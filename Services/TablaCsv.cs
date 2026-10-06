using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic.FileIO;

namespace osakaautomate.Services;

internal static class TablaCsv
{
    public static List<Dictionary<string, string>> Leer(string rutaArchivo)
    {
        if (string.IsNullOrWhiteSpace(rutaArchivo))
        {
            throw new ArgumentException("La ruta del archivo es obligatoria.", nameof(rutaArchivo));
        }

        if (!File.Exists(rutaArchivo))
        {
            throw new FileNotFoundException("No se encontró el archivo CSV.", rutaArchivo);
        }

        using var parser = new TextFieldParser(rutaArchivo, Encoding.UTF8)
        {
            TextFieldType = FieldType.Delimited,
            HasFieldsEnclosedInQuotes = true,
            TrimWhiteSpace = false
        };
        parser.SetDelimiters(";");

        string[]? encabezados = null;
        while (!parser.EndOfData)
        {
            var fields = parser.ReadFields();
            if (fields is not null && fields.Length > 1
                && string.Equals(fields[0].Trim(), "Id", StringComparison.OrdinalIgnoreCase))
            {
                encabezados = fields.Select(field => field.Trim()).ToArray();
                break;
            }
        }

        if (encabezados is null)
        {
            throw new InvalidDataException("No se encontró la fila de encabezados del CSV.");
        }

        var registros = new List<Dictionary<string, string>>();
        while (!parser.EndOfData)
        {
            var fields = parser.ReadFields();
            if (fields is null || fields.All(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            var record = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < encabezados.Length; index++)
            {
                record[encabezados[index]] = index < fields.Length ? fields[index].Trim() : string.Empty;
            }

            registros.Add(record);
        }

        return registros;
    }

    public static string Obtener(IReadOnlyDictionary<string, string> registro, string columna)
    {
        return registro.TryGetValue(columna, out var value) ? value.Trim() : string.Empty;
    }

    public static decimal ObtenerDecimal(IReadOnlyDictionary<string, string> registro, string columna)
    {
        var value = Obtener(registro, columna);
        if (string.IsNullOrWhiteSpace(value) || value.Equals("null", StringComparison.OrdinalIgnoreCase))
        {
            return 0m;
        }

        var lastComma = value.LastIndexOf(',');
        var lastPeriod = value.LastIndexOf('.');
        var normalized = lastComma >= 0 && lastPeriod >= 0
            ? lastComma > lastPeriod
                ? value.Replace(".", string.Empty, StringComparison.Ordinal).Replace(',', '.')
                : value.Replace(",", string.Empty, StringComparison.Ordinal)
            : value.Replace(',', '.');

        return decimal.TryParse(normalized, NumberStyles.Number | NumberStyles.AllowLeadingSign,
            CultureInfo.InvariantCulture, out var result)
            ? result
            : throw new InvalidDataException($"El valor '{value}' de la columna '{columna}' no es numérico.");
    }

    public static DateTime ObtenerFecha(IReadOnlyDictionary<string, string> registro, string columna)
    {
        var value = Obtener(registro, columna);
        if (DateTime.TryParseExact(value,
                ["d/M/yyyy H:mm", "d/M/yyyy H:mm:ss", "d/M/yyyy"],
                CultureInfo.GetCultureInfo("es-EC"),
                DateTimeStyles.AllowWhiteSpaces,
                out var result)
            || DateTime.TryParse(value, CultureInfo.GetCultureInfo("es-EC"), DateTimeStyles.AllowWhiteSpaces, out result))
        {
            return result;
        }

        throw new InvalidDataException($"La fecha '{value}' de la columna '{columna}' no es válida.");
    }

    public static string NormalizarNombreCliente(string nombre)
    {
        var descompuesto = (nombre ?? string.Empty).Trim().Normalize(NormalizationForm.FormD);
        var resultado = new StringBuilder(descompuesto.Length);

        foreach (var caracter in descompuesto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(caracter) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(caracter))
            {
                resultado.Append(char.ToUpperInvariant(caracter));
            }
        }

        return resultado.ToString();
    }
}

internal static class NormalizadorNumeroFactura
{
    private static readonly Regex ReferenciaRegex = new(
        @"(?<!\d)(?<establecimiento>\d{3})-(?<punto>\d{3})-(?<secuencial>\d{1,9}(?:/\d{1,9})*)(?!\d)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static string? Normalizar(string valor)
    {
        var match = ReferenciaRegex.Match(valor ?? string.Empty);
        if (!match.Success)
        {
            return null;
        }

        return CrearClave(match.Groups["establecimiento"].Value,
            match.Groups["punto"].Value,
            match.Groups["secuencial"].Value);
    }

    public static List<string> Extraer(string texto)
    {
        var facturas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (Match match in ReferenciaRegex.Matches(texto ?? string.Empty))
        {
            foreach (var secuencial in match.Groups["secuencial"].Value.Split('/'))
            {
                facturas.Add(CrearClave(
                    match.Groups["establecimiento"].Value,
                    match.Groups["punto"].Value,
                    secuencial));
            }
        }

        return facturas.ToList();
    }

    private static string CrearClave(string establecimiento, string punto, string secuencial)
    {
        return $"{establecimiento}-{punto}-{secuencial.PadLeft(9, '0')}";
    }
}
