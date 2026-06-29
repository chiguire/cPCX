using cpcx.Config;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace cpcx.Pages;

public class GuidelinesModel(IOptions<CpcxConfig> cpcxConfig) : PageModel
{
    public string CaretakerEmail => cpcxConfig.Value.CaretakerEmail;

    public void OnGet() { }
}

