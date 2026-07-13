using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;

namespace cpcx.Extensions;

public static class TextExtensions
{
    // Encodes the text for safe HTML output, then turns newlines into <br> tags.
    public static IHtmlContent ToHtmlWithLineBreaks(this string? text)
    {
        var encoded = HtmlEncoder.Default.Encode(text ?? "");
        var withBreaks = encoded.Replace("\r\n", "\n").Replace("\n", "<br>");
        return new HtmlString(withBreaks);
    }
}
