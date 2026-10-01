using osakaautomate.Dtos;

namespace osakaautomate.Services;

public class OcrNoConfiguradoService : IOCRService
{
    public Task<FacturaDto> ExtraerFacturaAsync(string rutaPdf, CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException(
            "Configure AZURE_DOCUMENT_INTELLIGENCE_ENDPOINT y AZURE_DOCUMENT_INTELLIGENCE_KEY para procesar facturas PDF.");
    }
}