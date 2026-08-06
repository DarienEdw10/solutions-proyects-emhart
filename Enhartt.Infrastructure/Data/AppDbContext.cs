using Microsoft.EntityFrameworkCore;
using Enhartt.Domain.Models;

namespace Enhartt.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Parametro> Parametros { get; set; }
        public DbSet<Maquina> Maquinas { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Parametro>(entity =>
            {
                entity.ToTable("parametros", schema: "emhart");
                entity.HasKey(e => e.IdentificadorId);
            });

            modelBuilder.Entity<Maquina>(entity =>
            {
                entity.ToTable("maquinas", schema: "emhart");
                entity.HasKey(e => e.Id);
            });
        }
    }
}