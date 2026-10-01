namespace osakaautomate.Dtos;

public class FacturaDto
{
    public string NumeroFactura { get; set; } = string.Empty;
    public string Vendedor { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
}