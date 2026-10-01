using System.ComponentModel.DataAnnotations;

namespace osakaautomate.Models;

public class Vendedor
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Nivel { get; set; } = string.Empty;

    public ICollection<FacturaProcesada> FacturasProcesadas { get; set; } = new List<FacturaProcesada>();
}