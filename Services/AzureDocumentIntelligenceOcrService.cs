using System.Globalization;
using System.IO;
using Azure;
using Azure.AI.DocumentIntelligence;
using osakaautomate.Dtos;

namespace osakaautomate.Services;

public class AzureDocumentIntelligenceOcrService : IOCRService
{
    private readonly DocumentIntelligenceClient _client;

    public AzureDocumentIntelligenceOcrService(string endpoint, string apiKey)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            throw new ArgumentException("El endpoint de Azure es obligatorio.", nameof(endpoint));
        }

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentException("La clave de Azure es obligatoria.", nameof(apiKey));
        }

        _client = new DocumentIntelligenceClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
    }

    public async Task<FacturaDto> ExtraerFacturaAsync(string rutaPdf, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rutaPdf))
        {
            throw new ArgumentException("La ruta del PDF es obligatoria.", nameof(rutaPdf));
        }

        if (!File.Exists(rutaPdf))
        {
            throw new FileNotFoundException("No se encontró el archivo PDF.", rutaPdf);
        }

        BinaryData contenidoPdf = await BinaryData.FromStreamAsync(
            File.OpenRead(rutaPdf),
            cancellationToken);

        Operation<AnalyzeResult> operation = await _client.AnalyzeDocumentAsync(
            WaitUntil.Completed,
            "prebuilt-invoice",
            contenidoPdf,
            cancellationToken: cancellationToken);

        var documento = operation.Value.Documents.FirstOrDefault()
            ?? throw new InvalidOperationException("No se pudo extraer información de la factura.");

        return new FacturaDto
        {
            NumeroFactura = ObtenerTexto(documento, "InvoiceId"),
            Vendedor = ObtenerTexto(documento, "VendorName"),
            Subtotal = ObtenerDecimal(documento, "SubTotal")
        };
    }

    private static string ObtenerTexto(AnalyzedDocument documento, string nombreCampo)
    {
        return documento.Fields.TryGetValue(nombreCampo, out var campo)
            ? campo.Content?.Trim() ?? string.Empty
            : string.Empty;
    }

    private static decimal ObtenerDecimal(AnalyzedDocument documento, string nombreCampo)
    {
        var texto = ObtenerTexto(documento, nombreCampo);

        return decimal.TryParse(texto, NumberStyles.Currency, CultureInfo.InvariantCulture, out var monto)
            ? monto
            : 0m;
    }
}