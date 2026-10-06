using osakaautomate.Dtos;

namespace osakaautomate.Services;

public interface ILectorFacturasCsvService
{
    List<FacturaCsvDto> LeerFacturas(string rutaArchivo);
}