using osakaautomate.Models;

namespace osakaautomate.Services;

public interface IExportadorPlantillaExcelService
{
    string ExportarComisiones(
        IEnumerable<FacturaProcesada> comisiones,
        string rutaPlantilla);
}