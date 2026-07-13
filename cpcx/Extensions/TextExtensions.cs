using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;

namespace cpcx.Extensions;

public static class TextExtensions
{
    // Splits on newlines and encodes each line for safe HTML output, then joins with <br> tags.
    public static IHtmlContent ToHtmlWithLineBreaks(this string? text)
    {
        var lines = (text ?? "").Replace("\r\n", "\n").Split('\n');
        var encodedLines = lines.Select(HtmlEncoder.Default.Encode);
        return new HtmlString(string.Join("<br>", encodedLines));
    }
}
