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
        await using var transaccion = await db.Database.BeginTransactionAsync(cancellationToken);

        var (ana, anaCreada) = await ObtenerOCrearUsuarioAsync(
            "ana@usmp.pe", "Ana Torres", "DEMO-ANA", "Odontología", 6);
        var (diego, diegoCreado) = await ObtenerOCrearUsuarioAsync(
            "diego@usmp.pe", "Diego Rojas", "DEMO-DIEGO", "Medicina", 8);
        var (valeria, valeriaCreada) = await ObtenerOCrearUsuarioAsync(
            "valeria@usmp.pe", "Valeria Paredes", "DEMO-VALERIA", "Ingeniería", 5);

        var materialesAna = await AgregarDatosUsuarioSiNoTieneMaterialesAsync(
            ana,
            id =>
            [
                CrearMaterial("Kit de exploración dental", "Espejo, pinza y explorador esterilizables, listos para prácticas preclínicas.", "Odontología", 1, CondicionMaterial.MuyBueno, true, "San Isidro", 18m, false, id),
                CrearMaterial("Articulador dental semiajustable", "Articulador completo para ejercicios de oclusión y montaje de modelos.", "Odontología", 1, CondicionMaterial.Bueno, false, "Santa Anita", 22m, false, id),
                CrearMaterial("Tipodonto para endodoncia", "Modelo dental didáctico para practicar accesos y preparación de conductos.", "Odontología", 3, CondicionMaterial.MuyBueno, false, "Pueblo Libre", 12m, false, id),
                CrearMaterial("Atlas de anatomía humana", "Atlas ilustrado de anatomía, edición universitaria en buen estado.", "Medicina", 4, CondicionMaterial.Bueno, false, "Cercado de Lima", 8m, false, id)
            ],
            id =>
            [
                CrearClase("Anatomía dental", "Repaso de nomenclatura, morfología y puntos de referencia dentales.", ModalidadClase.GoogleMeet, 25m, false, id),
                CrearClase("Materiales dentales", "Acompañamiento para reconocer propiedades y usos clínicos de materiales.", ModalidadClase.Zoom, 0m, true, id)
            ],
            cancellationToken);
        var materialesDiego = await AgregarDatosUsuarioSiNoTieneMaterialesAsync(
            diego,
            id =>
            [
                CrearMaterial("Estetoscopio clínico", "Estetoscopio de doble campana para prácticas de semiología.", "Medicina", 2, CondicionMaterial.MuyBueno, true, "Jesús María", 15m, false, id),
                CrearMaterial("Tensiómetro aneroide", "Equipo manual con brazalete adulto y estuche de transporte.", "Medicina", 2, CondicionMaterial.Bueno, true, "La Molina", 10m, false, id),
                CrearMaterial("Modelo anatómico de corazón", "Modelo desmontable a color para repasar cavidades y válvulas.", "Medicina", 3, CondicionMaterial.Bueno, false, "San Miguel", 0m, true, id),
                CrearMaterial("Calculadora científica", "Calculadora científica para cursos de cálculo, física y estadística.", "Ingeniería", 5, CondicionMaterial.MuyBueno, false, "Surco", 6m, false, id)
            ],
            id =>
            [
                CrearClase("Fisiología humana", "Sesión de resolución de preguntas sobre sistemas y homeostasis.", ModalidadClase.GoogleMeet, 20m, false, id),
                CrearClase("Anatomía: sistema cardiovascular", "Repaso solidario de corazón, circulación y vasos principales.", ModalidadClase.Zoom, 0m, true, id)
            ],
            cancellationToken);
        var materialesValeria = await AgregarDatosUsuarioSiNoTieneMaterialesAsync(
            valeria,
            id =>
            [
                CrearMaterial("Multímetro digital", "Multímetro con puntas de prueba y medición de voltaje, corriente y resistencia.", "Ingeniería", 6, CondicionMaterial.Bueno, false, "San Borja", 14m, false, id),
                CrearMaterial("Kit de prototipado Arduino", "Placa compatible, cables, protoboard y sensores básicos para laboratorio.", "Ingeniería", 6, CondicionMaterial.MuyBueno, false, "Miraflores", 20m, false, id),
                CrearMaterial("Manual de circuitos eléctricos", "Texto de consulta con ejercicios resueltos para circuitos I y II.", "Ingeniería", 4, CondicionMaterial.Bueno, false, "Lince", 0m, true, id),
                CrearMaterial("Calibrador Vernier", "Calibrador metálico de 150 mm con estuche, ideal para dibujo técnico.", "Ingeniería", 7, CondicionMaterial.Excelente, false, "Magdalena", 9m, false, id)
            ],
            id =>
            [
                CrearClase("Cálculo diferencial", "Ejercicios guiados de límites, derivadas y aplicaciones.", ModalidadClase.GoogleMeet, 18m, false, id),
                CrearClase("Introducción a circuitos", "Asesoría práctica para análisis de circuitos eléctricos básicos.", ModalidadClase.Zoom, 22m, false, id)
            ],
            cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        var fechaHoy = DateOnly.FromDateTime(DateTime.UtcNow);
        var alquileres = new List<Alquiler>();
        var resenas = new List<Resena>();

        if (materialesAna.Length > 0)
        {
            var alquiler = CrearAlquiler(materialesAna[0], diego, fechaHoy.AddDays(-12), fechaHoy.AddDays(-10), EstadoSolicitud.Devuelto);
            alquileres.Add(alquiler);
            resenas.Add(CrearResena(diego, ana, materialesAna[0], alquiler, 5, "El kit estaba limpio y en excelentes condiciones. Coordinamos la entrega sin problemas."));
        }

        if (materialesDiego.Length > 0)
        {
            var alquilerDevuelto = CrearAlquiler(materialesDiego[0], valeria, fechaHoy.AddDays(-8), fechaHoy.AddDays(-7), EstadoSolicitud.Devuelto);
            alquileres.Add(alquilerDevuelto);
            alquileres.Add(CrearAlquiler(materialesDiego[2], valeria, fechaHoy.AddDays(-4), fechaHoy.AddDays(-2), EstadoSolicitud.Activo));
            resenas.Add(CrearResena(valeria, diego, materialesDiego[0], alquilerDevuelto, 5, "El estetoscopio funcionó muy bien para mis prácticas clínicas."));
        }

        if (materialesValeria.Length > 0)
        {
            var alquilerDevuelto = CrearAlquiler(materialesValeria[2], ana, fechaHoy.AddDays(-6), fechaHoy.AddDays(-5), EstadoSolicitud.Devuelto);
            alquileres.Add(alquilerDevuelto);
            alquileres.Add(CrearAlquiler(materialesValeria[0], ana, fechaHoy.AddDays(2), fechaHoy.AddDays(3), EstadoSolicitud.Pendiente));
            resenas.Add(CrearResena(ana, valeria, materialesValeria[2], alquilerDevuelto, 4, "El manual estaba bien cuidado y me ayudó a preparar el curso."));
        }

        db.Alquileres.AddRange(alquileres);
        db.Resenas.AddRange(resenas);
        await db.SaveChangesAsync(cancellationToken);
        await transaccion.CommitAsync(cancellationToken);

        logger.LogInformation(
            "Datos demo procesados: {CantidadUsuarios} usuarios y {CantidadMateriales} materiales creados.",
            (anaCreada ? 1 : 0) + (diegoCreado ? 1 : 0) + (valeriaCreada ? 1 : 0),
            materialesAna.Length + materialesDiego.Length + materialesValeria.Length);
    }

    private async Task<(Usuario Usuario, bool Creado)> ObtenerOCrearUsuarioAsync(
        string correo,
        string nombre,
        string codigoAlumno,
        string carrera,
        int ciclo)
    {
        var usuarioExistente = await userManager.FindByEmailAsync(correo);
        if (usuarioExistente is not null)
        {
            return (usuarioExistente, false);
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

        return (usuario, true);
    }

    private async Task<Material[]> AgregarDatosUsuarioSiNoTieneMaterialesAsync(
        Usuario usuario,
        Func<int, Material[]> crearMateriales,
        Func<int, Clase[]> crearClases,
        CancellationToken cancellationToken)
    {
        if (await db.Materiales.AnyAsync(material => material.PropietarioId == usuario.Id, cancellationToken))
        {
            return [];
        }

        var materiales = crearMateriales(usuario.Id);
        db.Materiales.AddRange(materiales);
        db.Clases.AddRange(crearClases(usuario.Id));
        return materiales;
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
