using cpcx.Models;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.CodeAnalysis;

namespace cpcx.Extensions;

public static class MessageExtensions
{
    public static IHtmlContent StatusMessage<TModel>(this IHtmlHelper<TModel> helper, string fullStatusMessage)
    {
        var messageParts = fullStatusMessage.Split('%', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (!int.TryParse(messageParts[0], out var messageType))
        {
            throw new Exception("Status message incorrect format");
        }
        var message = messageParts[1];
        var messageCss = "";

        switch (messageType)
        {
            case StatusMessageType.Success:
                messageCss += "alert-success";
                break;

            case StatusMessageType.Warning:
                messageCss += "alert-warning";
                break;

            case StatusMessageType.Error:
                messageCss += "alert-danger";
                break;

            case StatusMessageType.Info:
                messageCss += "alert-info";
                break;
        }

        //First, build the innermost div tag, the one that displays the message
        TagBuilder display = new TagBuilder("div");
        display.AddCssClass("rounded p-lg-3");
        display.AddCssClass("alert " + messageCss);
        display.Attributes.Add("role", "alert");
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