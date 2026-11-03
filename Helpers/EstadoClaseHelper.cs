using UniLink.Models;

namespace UniLink.Helpers;

public static class EstadoClaseHelper
{
    public static string ObtenerEtiqueta(EstadoSolicitud estado) => estado switch
    {
        EstadoSolicitud.Aprobado => "Confirmada",
        EstadoSolicitud.Devuelto => "Dictada",
        EstadoSolicitud.Rechazado => "Rechazada",
        EstadoSolicitud.Pendiente => "Pendiente",
        EstadoSolicitud.Activo => "Activa",
        _ => "Pendiente"
    };

    public static string ObtenerClaseBadge(EstadoSolicitud estado) => estado switch
    {
        EstadoSolicitud.Aprobado => "estado-aprobado",
        EstadoSolicitud.Devuelto => "estado-devuelto",
        EstadoSolicitud.Rechazado => "estado-rechazado",
        EstadoSolicitud.Pendiente => "estado-pendiente",
        EstadoSolicitud.Activo => "estado-activo",
        _ => "estado-pendiente"
    };
}

public static class ModalidadClaseHelper
{
    public static string ObtenerEtiqueta(ModalidadClase modalidad) => modalidad switch
    {
        ModalidadClase.VideollamadaUniLink => "Videollamada UniLink (Jitsi)",
        ModalidadClase.GoogleMeet => "Google Meet",
        ModalidadClase.Zoom => "Zoom",
        _ => "Videollamada UniLink (Jitsi)"
    };
}
