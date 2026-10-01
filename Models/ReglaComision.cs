using System.ComponentModel.DataAnnotations;

namespace osakaautomate.Models;

public class ReglaComision
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string NivelVendedor { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string TipoProducto { get; set; } = string.Empty;

    [Range(0, double.MaxValue)]
    public decimal RangoVentaMinimo { get; set; }

    [Range(0, double.MaxValue)]
    public decimal RangoVentaMaximo { get; set; }

    [Range(0, 100)]
    public decimal PorcentajeComision { get; set; }
}