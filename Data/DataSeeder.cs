using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UniLink.Models;

namespace UniLink.Data;

public sealed class DataSeeder(
    UniLinkDbContext db,
    UserManager<Usuario> userManager,
    ILogger<DataSeeder> logger)
{
    private const string ContrasenaDemostracion = "Demo123!";

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await db.Materiales.AnyAsync(cancellationToken))
        {
            return;
        }

        await using var transaccion = await db.Database.BeginTransactionAsync(cancellationToken);

        var ana = await ObtenerOCrearUsuarioAsync(
            "ana@usmp.pe", "Ana Torres", "DEMO-ANA", "Odontología", 6);
        var diego = await ObtenerOCrearUsuarioAsync(
            "diego@usmp.pe", "Diego Rojas", "DEMO-DIEGO", "Medicina", 8);
        var valeria = await ObtenerOCrearUsuarioAsync(
            "valeria@usmp.pe", "Valeria Paredes", "DEMO-VALERIA", "Ingeniería", 5);

        var materiales = new[]
        {
            CrearMaterial("Kit de exploración dental", "Espejo, pinza y explorador esterilizables, listos para prácticas preclínicas.", "Odontología", 1, CondicionMaterial.MuyBueno, true, "San Isidro", 18m, false, ana.Id),
            CrearMaterial("Articulador dental semiajustable", "Articulador completo para ejercicios de oclusión y montaje de modelos.", "Odontología", 1, CondicionMaterial.Bueno, false, "Santa Anita", 22m, false, ana.Id),
            CrearMaterial("Tipodonto para endodoncia", "Modelo dental didáctico para practicar accesos y preparación de conductos.", "Odontología", 3, CondicionMaterial.MuyBueno, false, "Pueblo Libre", 12m, false, ana.Id),
            CrearMaterial("Atlas de anatomía humana", "Atlas ilustrado de anatomía, edición universitaria en buen estado.", "Medicina", 4, CondicionMaterial.Bueno, false, "Cercado de Lima", 8m, false, ana.Id),
            CrearMaterial("Estetoscopio clínico", "Estetoscopio de doble campana para prácticas de semiología.", "Medicina", 2, CondicionMaterial.MuyBueno, true, "Jesús María", 15m, false, diego.Id),
            CrearMaterial("Tensiómetro aneroide", "Equipo manual con brazalete adulto y estuche de transporte.", "Medicina", 2, CondicionMaterial.Bueno, true, "La Molina", 10m, false, diego.Id),
            CrearMaterial("Modelo anatómico de corazón", "Modelo desmontable a color para repasar cavidades y válvulas.", "Medicina", 3, CondicionMaterial.Bueno, false, "San Miguel", 0m, true, diego.Id),
            CrearMaterial("Calculadora científica", "Calculadora científica para cursos de cálculo, física y estadística.", "Ingeniería", 5, CondicionMaterial.MuyBueno, false, "Surco", 6m, false, diego.Id),
            CrearMaterial("Multímetro digital", "Multímetro con puntas de prueba y medición de voltaje, corriente y resistencia.", "Ingeniería", 6, CondicionMaterial.Bueno, false, "San Borja", 14m, false, valeria.Id),
            CrearMaterial("Kit de prototipado Arduino", "Placa compatible, cables, protoboard y sensores básicos para laboratorio.", "Ingeniería", 6, CondicionMaterial.MuyBueno, false, "Miraflores", 20m, false, valeria.Id),
            CrearMaterial("Manual de circuitos eléctricos", "Texto de consulta con ejercicios resueltos para circuitos I y II.", "Ingeniería", 4, CondicionMaterial.Bueno, false, "Lince", 0m, true, valeria.Id),
            CrearMaterial("Calibrador Vernier", "Calibrador metálico de 150 mm con estuche, ideal para dibujo técnico.", "Ingeniería", 7, CondicionMaterial.Excelente, false, "Magdalena", 9m, false, valeria.Id)
        };

        db.Materiales.AddRange(materiales);
        await db.SaveChangesAsync(cancellationToken);

        var clases = new[]
        {
            CrearClase("Anatomía dental", "Repaso de nomenclatura, morfología y puntos de referencia dentales.", ModalidadClase.GoogleMeet, 25m, false, ana.Id),
            CrearClase("Materiales dentales", "Acompañamiento para reconocer propiedades y usos clínicos de materiales.", ModalidadClase.Zoom, 0m, true, ana.Id),
            CrearClase("Fisiología humana", "Sesión de resolución de preguntas sobre sistemas y homeostasis.", ModalidadClase.GoogleMeet, 20m, false, diego.Id),
            CrearClase("Anatomía: sistema cardiovascular", "Repaso solidario de corazón, circulación y vasos principales.", ModalidadClase.Zoom, 0m, true, diego.Id),
            CrearClase("Cálculo diferencial", "Ejercicios guiados de límites, derivadas y aplicaciones.", ModalidadClase.GoogleMeet, 18m, false, valeria.Id),
            CrearClase("Introducción a circuitos", "Asesoría práctica para análisis de circuitos eléctricos básicos.", ModalidadClase.Zoom, 22m, false, valeria.Id)
        };

        db.Clases.AddRange(clases);
        await db.SaveChangesAsync(cancellationToken);

        var fechaHoy = DateOnly.FromDateTime(DateTime.UtcNow);
        var alquileres = new[]
        {
            CrearAlquiler(materiales[0], diego, fechaHoy.AddDays(-12), fechaHoy.AddDays(-10), EstadoSolicitud.Devuelto),
            CrearAlquiler(materiales[4], valeria, fechaHoy.AddDays(-8), fechaHoy.AddDays(-7), EstadoSolicitud.Devuelto),
            CrearAlquiler(materiales[10], ana, fechaHoy.AddDays(-6), fechaHoy.AddDays(-5), EstadoSolicitud.Devuelto),
            CrearAlquiler(materiales[8], ana, fechaHoy.AddDays(2), fechaHoy.AddDays(3), EstadoSolicitud.Pendiente),
            CrearAlquiler(materiales[6], valeria, fechaHoy.AddDays(-4), fechaHoy.AddDays(-2), EstadoSolicitud.Activo)
        };

        db.Alquileres.AddRange(alquileres);
        await db.SaveChangesAsync(cancellationToken);

        db.Resenas.AddRange(
            CrearResena(diego, ana, materiales[0], alquileres[0], 5, "El kit estaba limpio y en excelentes condiciones. Coordinamos la entrega sin problemas."),
            CrearResena(valeria, diego, materiales[4], alquileres[1], 5, "El estetoscopio funcionó muy bien para mis prácticas clínicas."),
            CrearResena(ana, valeria, materiales[10], alquileres[2], 4, "El manual estaba bien cuidado y me ayudó a preparar el curso."));
        await db.SaveChangesAsync(cancellationToken);
        await transaccion.CommitAsync(cancellationToken);

        logger.LogInformation(
            "Se cargaron datos de demostración: {CantidadMateriales} materiales, {CantidadClases} clases y {CantidadResenas} reseñas.",
            materiales.Length,
            clases.Length,
            3);
    }

    private async Task<Usuario> ObtenerOCrearUsuarioAsync(
        string correo,
        string nombre,
        string codigoAlumno,
        string carrera,
        int ciclo)
    {
        var usuarioExistente = await userManager.FindByEmailAsync(correo);
        if (usuarioExistente is not null)
        {
            return usuarioExistente;
        }

        var usuario = new Usuario
        {
            UserName = correo,
            Email = correo,
            EmailConfirmed = true,
            Nombre = nombre,
            CodigoAlumno = codigoAlumno,
            Carrera = carrera,
            Ciclo = ciclo,
            FechaRegistro = DateTime.UtcNow
        };

        var resultado = await userManager.CreateAsync(usuario, ContrasenaDemostracion);
        if (!resultado.Succeeded)
        {
            var errores = string.Join("; ", resultado.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"No se pudo crear el usuario de demostración {correo}: {errores}");
        }

        return usuario;
    }

    private static Material CrearMaterial(
        string nombre,
        string descripcion,
        string carrera,
        int categoriaId,
        CondicionMaterial condicion,
        bool esterilizado,
        string ubicacion,
        decimal precioDia,
        bool esSolidario,
        int propietarioId) => new()
        {
            Nombre = nombre,
            Descripcion = descripcion,
            Carrera = carrera,
            CategoriaId = categoriaId,
            Condicion = condicion,
            Esterilizado = esterilizado,
            Ubicacion = ubicacion,
            PrecioDia = precioDia,
            EsSolidario = esSolidario,
            Disponible = true,
            PropietarioId = propietarioId,
            FechaPublicacion = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 25))
        };

    private static Clase CrearClase(
        string curso,
        string descripcion,
        ModalidadClase modalidad,
        decimal precioHora,
        bool esSolidaria,
        int tutorId) => new()
        {
            Curso = curso,
            Descripcion = descripcion,
            Modalidad = modalidad,
            PrecioHora = precioHora,
            EsSolidaria = esSolidaria,
            TutorId = tutorId,
            FechaPublicacion = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 15))
        };

    private static Alquiler CrearAlquiler(
        Material material,
        Usuario solicitante,
        DateOnly fechaInicio,
        DateOnly fechaFin,
        EstadoSolicitud estado)
    {
        var dias = fechaFin.DayNumber - fechaInicio.DayNumber + 1;
        return new Alquiler
        {
            Material = material,
            Solicitante = solicitante,
            FechaInicio = fechaInicio,
            FechaFin = fechaFin,
            CostoTotal = dias * material.PrecioDia,
            Estado = estado,
            FechaSolicitud = DateTime.UtcNow.AddDays(-Math.Max(1, DateOnly.FromDateTime(DateTime.UtcNow).DayNumber - fechaInicio.DayNumber))
        };
    }

    private static Resena CrearResena(
        Usuario autor,
        Usuario usuarioResenado,
        Material material,
        Alquiler? alquiler,
        int calificacion,
        string comentario) => new()
        {
            Autor = autor,
            UsuarioReseñado = usuarioResenado,
            Material = material,
            Alquiler = alquiler,
            Calificacion = calificacion,
            Comentario = comentario,
            Sentimiento = "Positivo",
            Probabilidad = 0.96f,
            Fecha = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 6))
        };
}
