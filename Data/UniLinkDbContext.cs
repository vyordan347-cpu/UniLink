using Microsoft.EntityFrameworkCore;
using UniLink.Models;

namespace UniLink.Data;

public class UniLinkDbContext(DbContextOptions<UniLinkDbContext> options) : DbContext(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Material> Materiales => Set<Material>();
    public DbSet<Alquiler> Alquileres => Set<Alquiler>();
    public DbSet<Clase> Clases => Set<Clase>();
    public DbSet<ReservaClase> ReservasClase => Set<ReservaClase>();
    public DbSet<Notificacion> Notificaciones => Set<Notificacion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Usuario>()
            .HasIndex(usuario => usuario.Correo)
            .IsUnique();

        modelBuilder.Entity<Usuario>()
            .HasIndex(usuario => usuario.CodigoAlumno)
            .IsUnique();

        modelBuilder.Entity<Material>()
            .HasOne(material => material.Propietario)
            .WithMany(usuario => usuario.MaterialesPublicados)
            .HasForeignKey(material => material.PropietarioId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Material>()
            .HasOne(material => material.Categoria)
            .WithMany(categoria => categoria.Materiales)
            .HasForeignKey(material => material.CategoriaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Alquiler>()
            .HasOne(alquiler => alquiler.Material)
            .WithMany(material => material.Alquileres)
            .HasForeignKey(alquiler => alquiler.MaterialId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Alquiler>()
            .HasOne(alquiler => alquiler.Solicitante)
            .WithMany(usuario => usuario.SolicitudesAlquiler)
            .HasForeignKey(alquiler => alquiler.SolicitanteId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Clase>()
            .HasOne(clase => clase.Tutor)
            .WithMany(usuario => usuario.ClasesPublicadas)
            .HasForeignKey(clase => clase.TutorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ReservaClase>()
            .HasOne(reserva => reserva.Clase)
            .WithMany(clase => clase.Reservas)
            .HasForeignKey(reserva => reserva.ClaseId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ReservaClase>()
            .HasOne(reserva => reserva.Alumno)
            .WithMany(usuario => usuario.ReservasClase)
            .HasForeignKey(reserva => reserva.AlumnoId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Notificacion>()
            .HasOne(notificacion => notificacion.Usuario)
            .WithMany(usuario => usuario.Notificaciones)
            .HasForeignKey(notificacion => notificacion.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Categoria>().HasData(
            new Categoria { Id = 1, Nombre = "Instrumental odontológico" },
            new Categoria { Id = 2, Nombre = "Equipos médicos" },
            new Categoria { Id = 3, Nombre = "Modelos anatómicos" },
            new Categoria { Id = 4, Nombre = "Libros" },
            new Categoria { Id = 5, Nombre = "Calculadoras" },
            new Categoria { Id = 6, Nombre = "Tecnología" },
            new Categoria { Id = 7, Nombre = "Otros" });
    }
}