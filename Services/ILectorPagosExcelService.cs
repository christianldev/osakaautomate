namespace osakaautomate.Services;

public interface ILectorPagosExcelService
{
    List<string> LeerFacturasPagadas(string rutaArchivo);
}