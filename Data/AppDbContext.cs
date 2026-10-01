using Microsoft.EntityFrameworkCore;
using osakaautomate.Models;

namespace osakaautomate.Data;

public class AppDbContext : DbContext
{
    public DbSet<Vendedor> Vendedores => Set<Vendedor>();
    public DbSet<ReglaComision> ReglasComision => Set<ReglaComision>();
    public DbSet<FacturaProcesada> FacturasProcesadas => Set<FacturaProcesada>();

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlite("Data Source=comisiones.db");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<FacturaProcesada>()
            .HasOne(factura => factura.Vendedor)
            .WithMany(vendedor => vendedor.FacturasProcesadas)
            .HasForeignKey(factura => factura.VendedorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Vendedor>().HasData(
            new Vendedor { Id = 1, Nombre = "Ana Garcia", Nivel = "Junior" },
            new Vendedor { Id = 2, Nombre = "Carlos Martinez", Nivel = "Senior" });

        modelBuilder.Entity<ReglaComision>().HasData(
            new ReglaComision { Id = 1, NivelVendedor = "Junior", TipoProducto = "General", RangoVentaMinimo = 0m, RangoVentaMaximo = 9999.99m, PorcentajeComision = 3m },
            new ReglaComision { Id = 2, NivelVendedor = "Junior", TipoProducto = "General", RangoVentaMinimo = 10000m, RangoVentaMaximo = 999999.99m, PorcentajeComision = 5m },
            new ReglaComision { Id = 3, NivelVendedor = "Senior", TipoProducto = "General", RangoVentaMinimo = 0m, RangoVentaMaximo = 9999.99m, PorcentajeComision = 5m },
            new ReglaComision { Id = 4, NivelVendedor = "Senior", TipoProducto = "General", RangoVentaMinimo = 10000m, RangoVentaMaximo = 999999.99m, PorcentajeComision = 7m });
    }
}