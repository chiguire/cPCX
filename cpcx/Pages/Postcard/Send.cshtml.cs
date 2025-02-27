using cpcx.Entities;
using cpcx.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace cpcx.Pages.Postcard;

public class Send(MainEventService mainEventService,
                  UserManager<CpcxUser> userManager,
                  IEventService eventService, 
                  IPostcardService postcardService) : PageModel
{
    public IActionResult OnGet()
    {
        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        var eventId = await mainEventService.GetMainEventId();
        var e = await eventService.GetEvent(eventId);
        //await postcardService.SendPostcard(HttpContext.User.FindFirst((ClaimTypes.Name)))
        
        return Page();
    }
}