namespace osakaautomate.Services;

public interface ILectorHistoricoExcelService
{
    Dictionary<string, string> LeerCiudadesPorCliente(string rutaArchivo);
}