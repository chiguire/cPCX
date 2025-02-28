using cpcx.Exceptions;
using cpcx.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace cpcx.Pages.Postcard;

[Authorize]
public class Index(IPostcardService postcardService) : PageModel
{
    public string PostcardId { get; set; } = null!;
    public Entities.Postcard Postcard { get; set; } = null!; 
    
    public async Task<IActionResult> OnGet(string postcardId)
    {
        try
        {
            var p = await postcardService.GetPostcard(postcardId);

            PostcardId = postcardId;
            Postcard = p;

            return Page();
        }
        catch (CPCXException e)
        {
            // TODO - Set status message
            return RedirectToPage("/Index");
        }
    }
}