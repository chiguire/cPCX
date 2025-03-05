using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace cpcx.Models;

public class MessagePageModel : PageModel
{
    [DisplayName("Message: ")]
    [TempData]
    public string? StatusMessage { get; set; }

    [DisplayName("Type: ")]
    [TempData]
    public int? StatusMessageCssType { get; set; }
    
    public void SetStatusMessage(string message, int type)
    {
        StatusMessage = message;
        StatusMessageCssType = type;
    }
}