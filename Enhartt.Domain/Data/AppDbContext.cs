using Microsoft.EntityFrameworkCore;
using Enhartt.Domain.Models;

namespace Enhartt.Domain.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Parametro> Parametros { get; set; }
        public DbSet<Maquina> Maquinas { get; set; }
        public DbSet<Receta> Receta { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            bool esSqlite = Database.IsSqlite();
            string? esquema = esSqlite ? null : "emhart";

            if (!esSqlite)
            {
                modelBuilder.HasDefaultSchema("emhart");
            }

            modelBuilder.Entity<Parametro>(entity =>
            {
                entity.ToTable("parametros", schema: esquema);
                entity.HasKey(e => e.IdRegistro);

                entity.HasOne(p => p.Maquina)
                      .WithMany()
                      .HasForeignKey(p => p.IdentificadorId)
                      .HasConstraintName("FK_parametros_maquinas");
            });

            modelBuilder.Entity<Maquina>(entity =>
            {
                entity.ToTable("maquinas", schema: esquema);
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.IdMaquina).IsUnique();
            });

            modelBuilder.Entity<Receta>(entity =>
            {
                entity.ToTable("receta", schema: esquema);
                entity.HasKey(e => e.IdReferencia);
            });

            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.ToTable("usuarios", schema: esquema);
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.CWID).IsUnique();
            });
        }
    }
}