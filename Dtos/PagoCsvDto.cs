namespace osakaautomate.Dtos;

public class PagoCsvDto
{
    public string Tipo { get; set; } = string.Empty;
    public string CodigoCliente { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public DateTime FechaPago { get; set; }
    public bool EsNotaCredito { get; set; }
    public List<string> Facturas { get; set; } = [];
}