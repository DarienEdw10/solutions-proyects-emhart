using Microsoft.EntityFrameworkCore;
using Enhartt.Domain.Models;

namespace Enhartt.Domain.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Parametro> Parametros { get; set; }
        public DbSet<Maquina> Maquinas { get; set; }
        public DbSet<Receta> Receta { get; set; }//quite una s
        public DbSet<Usuario> Usuarios { get; set; }

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

            modelBuilder.Entity<Receta>(entity =>
            {
                entity.ToTable("receta", schema: "emhart");
                entity.HasKey(e => e.IdReferencia);

                // Se elimina la relación directa FK con Maquina para evitar fallos de mapeo
            });
            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.ToTable("usuarios", schema: "emhart");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.CWID).IsUnique();
            });
        }
    }
}