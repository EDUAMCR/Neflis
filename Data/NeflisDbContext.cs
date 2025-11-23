using Microsoft.EntityFrameworkCore;
using Neflis.Models;

namespace Neflis.Data
{
    public class NeflisDbContext : DbContext
    {
        public NeflisDbContext(DbContextOptions<NeflisDbContext> options)
            : base(options)
        {
        }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Perfil> Perfiles { get; set; }
        public DbSet<PlanSuscripcion> PlanesSuscripcion { get; set; }
        public DbSet<SuscripcionUsuario> SuscripcionesUsuario { get; set; }
        public DbSet<MetodoPago> MetodosPago { get; set; }
        public DbSet<Contenido> Contenidos { get; set; }
        public DbSet<MiLista> MiLista { get; set; }
        public DbSet<HistorialPago> HistorialPagos { get; set; }
        public DbSet<Notificacion> Notificaciones { get; set; }
        public DbSet<ComentarioContenido> ComentariosContenido { get; set; }
        public DbSet<CalificacionContenido> CalificacionesContenido { get; set; }



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // correo único
            modelBuilder.Entity<Usuario>()
                .HasIndex(u => u.Correo)
                .IsUnique();

            // relación usuario-perfil
            modelBuilder.Entity<Perfil>()
                .HasOne(p => p.Usuario)
                .WithMany(u => u.Perfiles)
                .HasForeignKey(p => p.UsuarioId);

            // relación usuario-suscripción
            modelBuilder.Entity<SuscripcionUsuario>()
                .HasOne(s => s.Usuario)
                .WithMany(u => u.Suscripciones)
                .HasForeignKey(s => s.UsuarioId);

            modelBuilder.Entity<SuscripcionUsuario>()
                .HasOne(s => s.PlanSuscripcion)
                .WithMany()
                .HasForeignKey(s => s.PlanSuscripcionId);

            // relación usuario-método de pago
            modelBuilder.Entity<MetodoPago>()
                .HasOne(m => m.Usuario)
                .WithMany(u => u.MetodosPago)
                .HasForeignKey(m => m.UsuarioId);
            // relacion un contenido - muchas calificaciones
            modelBuilder.Entity<CalificacionContenido>()
                .HasOne(c => c.Contenido)
                .WithMany(c => c.Calificaciones)
                .HasForeignKey(c => c.ContenidoId);

            // índice único Contenido + Perfil
            modelBuilder.Entity<CalificacionContenido>()
                .HasIndex(c => new { c.ContenidoId, c.PerfilId })
                .IsUnique();

            // relación calificación-perfil
            modelBuilder.Entity<CalificacionContenido>()
                .HasOne(c => c.Perfil)
                .WithMany()
                .HasForeignKey(c => c.PerfilId)
                .OnDelete(DeleteBehavior.Cascade);

            // relación calificación-usuario (para satisfacer la FK y NOT NULL)
            modelBuilder.Entity<CalificacionContenido>()
                .HasOne(c => c.Usuario)
                .WithMany()
                .HasForeignKey(c => c.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}

