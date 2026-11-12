using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Configuration;
using UniLink.Models;

namespace UniLink.Services;

public sealed class EnvioCorreoResend(
    HttpClient httpClient,
    IConfiguration configuration)
{
    private const string Remitente = "UniLink <onboarding@resend.dev>";

    public async Task EnviarAsync(CorreoMensaje correo, CancellationToken cancellationToken)
    {
        var apiKey = configuration["Resend:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("No se configuró Resend:ApiKey.");
        }

        var correoDemo = configuration["Resend:CorreoDemo"]?.Trim();
        var destinatarioReal = correo.Destinatario;
        var destinatarioEnvio = string.IsNullOrWhiteSpace(correoDemo) ? destinatarioReal : correoDemo;
        var asunto = string.IsNullOrWhiteSpace(correoDemo)
            ? correo.Asunto
            : $"[para {destinatarioReal}] {correo.Asunto}";

        using var solicitud = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails");
        solicitud.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        solicitud.Content = JsonContent.Create(new
        {
            from = Remitente,
            to = new[] { destinatarioEnvio },
            subject = asunto,
            html = CrearHtml(correo)
        });

        using var respuesta = await httpClient.SendAsync(solicitud, cancellationToken);
        if (!respuesta.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Resend rechazó el correo con el estado HTTP {(int)respuesta.StatusCode}.");
        }
    }

    private static string CrearHtml(CorreoMensaje correo)
    {
        var (tituloPorDefecto, mensajePorDefecto) = correo.Plantilla switch
        {
            "solicitud-alquiler-recibida" => ("Nueva solicitud de alquiler", "Tienes una nueva solicitud de alquiler."),
            "solicitud-alquiler-aprobada" => ("Solicitud aprobada", "Tu solicitud de alquiler fue aprobada."),
            "solicitud-alquiler-rechazada" => ("Solicitud rechazada", "Tu solicitud de alquiler fue rechazada."),
            "reserva-clase-confirmada" => ("Reserva de clase confirmada", "Tu reserva de clase fue confirmada."),
            "alquiler-vence-manana" => ("Tu alquiler vence mañana", "Recuerda devolver tu material mañana."),
            "prueba" => ("La cola de correos funciona", "Este es un correo de prueba de UniLink."),
            _ => throw new InvalidOperationException("La plantilla del correo no está reconocida.")
        };
        var titulo = Html(correo.Datos.GetValueOrDefault("Titulo", tituloPorDefecto));
        var mensaje = Html(correo.Datos.GetValueOrDefault("Mensaje", mensajePorDefecto));
        var botonTexto = Html(correo.Datos.GetValueOrDefault("BotonTexto", "Ver detalle"));
        var botonUrl = Html(ValidarUrl(correo.Datos.GetValueOrDefault("BotonUrl")));
        var enlaceReunion = correo.Datos.GetValueOrDefault("EnlaceReunion");
        var reunionHtml = string.IsNullOrWhiteSpace(enlaceReunion)
            ? string.Empty
            : $"""
              <p style="margin:24px 0 8px;color:#1E2235;font-weight:600">Enlace de la clase</p>
              <p style="margin:0"><a href="{Html(ValidarUrl(enlaceReunion))}" style="color:#3B4CCA;word-break:break-all">{Html(enlaceReunion)}</a></p>
              """;

        return $"""
            <!doctype html>
            <html lang="es">
            <head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"></head>
            <body style="margin:0;padding:24px;background:#F7F8FC;font-family:Arial,sans-serif;color:#1E2235">
              <table role="presentation" width="100%" cellspacing="0" cellpadding="0">
                <tr><td align="center">
                  <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="max-width:600px;background:#FFFFFF;border:1px solid #E5E7EB;border-radius:12px">
                    <tr><td style="padding:32px">
                      <p style="margin:0 0 24px;color:#3B4CCA;font-size:20px;font-weight:700">UniLink<span style="color:#14B8A6">.</span></p>
                      <h1 style="margin:0 0 16px;font-size:24px;line-height:1.3">{titulo}</h1>
                      <p style="margin:0;color:#6B7280;font-size:16px;line-height:1.6">{mensaje}</p>
                      {reunionHtml}
                      <p style="margin:28px 0 0">
                        <a href="{botonUrl}" style="display:inline-block;padding:12px 20px;border-radius:10px;background:#14B8A6;color:#1E2235;font-weight:700;text-decoration:none">{botonTexto}</a>
                      </p>
                      <p style="margin:32px 0 0;color:#6B7280;font-size:14px">Conecta, comparte y aprende.</p>
                    </td></tr>
                  </table>
                </td></tr>
              </table>
            </body>
            </html>
            """;
    }

    private static string ValidarUrl(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)
            ? uri.AbsoluteUri
            : "#";

    private static string Html(string? valor) =>
        HtmlEncoder.Default.Encode(valor ?? string.Empty);
}
