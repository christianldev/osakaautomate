using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace osakaautomate.Models;

public class FacturaProcesada
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string NumeroFactura { get; set; } = string.Empty;

    [Required]
    [ForeignKey(nameof(Vendedor))]
    public int VendedorId { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Subtotal { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal MontoComision { get; set; }

    [Required]
    [StringLength(20)]
    public string Estado { get; set; } = string.Empty;

    public Vendedor Vendedor { get; set; } = null!;
}