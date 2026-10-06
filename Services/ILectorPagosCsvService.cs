using osakaautomate.Dtos;

namespace osakaautomate.Services;

public interface ILectorPagosCsvService
{
    List<PagoCsvDto> LeerPagos(string rutaArchivo);
}