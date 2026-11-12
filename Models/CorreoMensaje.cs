namespace UniLink.Models;

public sealed record CorreoMensaje(
    string Destinatario,
    string Asunto,
    string Plantilla,
    Dictionary<string, string> Datos);
