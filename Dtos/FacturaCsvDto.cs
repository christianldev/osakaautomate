namespace osakaautomate.Dtos;

public class FacturaCsvDto
{
    public string NumeroFactura { get; set; } = string.Empty;
    public string CodigoCliente { get; set; } = string.Empty;
    public string Cliente { get; set; } = string.Empty;
    public string Sucursal { get; set; } = string.Empty;
    public string Vendedor { get; set; } = string.Empty;
    public string CodigoVendedor { get; set; } = string.Empty;
    public DateTime FechaFactura { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Descuento { get; set; }
    public decimal Iva { get; set; }
    public decimal Total { get; set; }
}