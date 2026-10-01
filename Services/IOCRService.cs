using osakaautomate.Dtos;

namespace osakaautomate.Services;

public interface IOCRService
{
    Task<FacturaDto> ExtraerFacturaAsync(string rutaPdf, CancellationToken cancellationToken = default);
}