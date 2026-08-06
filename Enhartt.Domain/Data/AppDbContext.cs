using Microsoft.EntityFrameworkCore;
using Enhartt.Domain.Models;

namespace Enhartt.Domain.Data // Actualizado a Enhartt.Domain
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Parametro> Parametros { get; set; }
        public DbSet<Maquina> Maquinas { get; set; }
        public DbSet<ReferenciaTolerancia> ReferenciasTolerancia { get; set; }
        public DbSet<HistorialReceta> HistorialRecetas { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.HasDefaultSchema("emhart");

            modelBuilder.Entity<Parametro>(entity =>
            {
                entity.ToTable("parametros", schema: "emhart");
                entity.HasKey(e => e.IdRegistro);

                entity.HasOne(p => p.Maquina)
                      .WithMany()
                      .HasForeignKey(p => p.IdentificadorId)
                      .HasConstraintName("FK_parametros_maquinas");
            });

            modelBuilder.Entity<Maquina>(entity =>
            {
                entity.ToTable("maquinas", schema: "emhart");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.IdMaquina).IsUnique();
            });

            modelBuilder.Entity<ReferenciaTolerancia>(entity =>
            {
                entity.ToTable("referencia_tolerancia", schema: "emhart");
                entity.HasKey(e => e.IdReferencia);

                entity.HasOne(r => r.Maquina)
                      .WithMany()
                      .HasForeignKey(r => r.IdMaquina)
                      .HasConstraintName("FK_referencia_tolerancia_maquinas");
            });
        }
    }
}