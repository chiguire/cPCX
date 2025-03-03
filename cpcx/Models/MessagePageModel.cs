using System.ComponentModel;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace cpcx.Models;

public class MessagePageModel : PageModel
{
    [DisplayName("Message: ")]
    public string? Message { get; set; }

    [DisplayName("Type: ")]
    public FlashMessageType? MessageCssType { get; set; }
    
    public void SetStatusMessage(string message, FlashMessageType type)
    {
        Message = message;
        MessageCssType = type;
    }
}