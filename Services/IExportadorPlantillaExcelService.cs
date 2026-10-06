using osakaautomate.Dtos;

namespace osakaautomate.Services;

public interface IExportadorPlantillaExcelService
{
    string ExportarComisiones(
        IEnumerable<ComisionReporteDto> comisiones,
        string rutaHistorico,
        string rutaArchivo);
}