using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace cpcx.Models;

public class MessagePageModel : PageModel
{
    [TempData] public string? StatusMessage { get; set; } = null;
    
    public void SetStatusMessage(string message, int type)
    {
        StatusMessage = $"{type}%{message}";
    }
}