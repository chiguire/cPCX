using cpcx.Models;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace cpcx.Extensions;

public static class MessageExtensions
{
    public static IHtmlContent StatusMessage<TModel>(this IHtmlHelper<TModel> helper, string message, FlashMessageType messageType)
    {
        string messageCss = "";

        switch (messageType)
        {
            case FlashMessageType.Success:
                messageCss += "alert-success";
                break;

            case FlashMessageType.Warning:
                messageCss += "alert-warning";
                break;

            case FlashMessageType.Error:
                messageCss += "alert-danger";
                break;

            case FlashMessageType.Info:
                messageCss += "alert-info";
                break;
        }

        //First, build the innermost div tag, the one that displays the message
        TagBuilder display = new TagBuilder("div");
        display.AddCssClass("rounded p-lg-3");
        display.AddCssClass(messageCss);
        display.InnerHtml.Append(message);

        //Now, build the wrapping column
        TagBuilder columns = new TagBuilder("div");
        columns.AddCssClass("col-lg");
        columns.InnerHtml.AppendHtml(display);

        //Finally, build the wrapping row
        TagBuilder row = new TagBuilder("div");
        row.AddCssClass("row");
        row.InnerHtml.AppendHtml(columns);

        return row;
    }
}