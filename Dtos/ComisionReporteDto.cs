namespace osakaautomate.Dtos;

public class ComisionReporteDto
{
    public string Cliente { get; set; } = string.Empty;
    public string Sucursal { get; set; } = string.Empty;
    public string NumeroFactura { get; set; } = string.Empty;
    public string Vendedor { get; set; } = string.Empty;
    public DateTime FechaFactura { get; set; }
    public decimal VentaBruta { get; set; }
    public decimal Descuento { get; set; }
    public decimal TotalCobrado { get; set; }
    public decimal Iva { get; set; }
    public DateTime FechaPago { get; set; }
    public decimal CobroNeto { get; set; }
    public int Dias { get; set; }
    public decimal PorcentajeComision { get; set; }
    public decimal TotalComision { get; set; }
    public decimal Retenciones { get; set; }
}