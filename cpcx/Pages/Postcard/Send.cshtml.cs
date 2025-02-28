using cpcx.Entities;
using cpcx.Exceptions;
using cpcx.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace cpcx.Pages.Postcard;

[Authorize]
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
        var us = await userManager.GetUserAsync(User);
        var eventId = await mainEventService.GetMainEventId();
        var ev = await eventService.GetEvent(eventId);

        Entities.Postcard? postcard = null;

        try
        {
            postcard = await postcardService.SendPostcard(us!, ev!);
        }
        catch (CPCXException e)
        {
            // TODO - Set status message
            return Page();
        }

        return RedirectToPage("/Postcard/Index", new { postcardId = postcard.Id });
    }
}