using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace cpcx.Pages;

public class GuidelinesModel : PageModel
{
    private readonly ILogger<GuidelinesModel> _logger;

    public GuidelinesModel(ILogger<GuidelinesModel> logger)
    {
        _logger = logger;
    }

    public void OnGet()
    {
    }
}

