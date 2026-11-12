using System.Globalization;
using System.Text;

namespace UniLink.Helpers;

public static class TextoBusqueda
{
    public static string NormalizarSinTildes(string texto)
    {
        var textoDescompuesto = texto.Normalize(NormalizationForm.FormD);
        var resultado = new StringBuilder(textoDescompuesto.Length);

        foreach (var caracter in textoDescompuesto)
        {
            var categoria = CharUnicodeInfo.GetUnicodeCategory(caracter);
            if (categoria is not UnicodeCategory.NonSpacingMark
                and not UnicodeCategory.SpacingCombiningMark
                and not UnicodeCategory.EnclosingMark)
            {
                resultado.Append(caracter);
            }
        }

        return resultado.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
    }
}
