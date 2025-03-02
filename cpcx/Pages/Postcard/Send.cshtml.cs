using cpcx.Config;
using cpcx.Entities;
using cpcx.Exceptions;
using cpcx.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace cpcx.Pages.Postcard;

[Authorize]
public class Send(MainEventService mainEventService,
                  IUserService userService,
                  UserManager<CpcxUser> userManager,
                  IEventService eventService, 
                  IOptionsSnapshot<PostcardConfig> postcardConfig,
                  IPostcardService postcardService) : PageModel
{
    private readonly PostcardConfig _postcardConfig = postcardConfig.Value;
    
    public int TravellingPostcards { get; set; }
    public int MaxTravellingPostcards { get; set; }
    public bool CanUserSendPostcards { get; set; }
    
    public async Task<IActionResult> OnGet()
    {
        var us = await userManager.GetUserAsync(User);
        var eventId = await mainEventService.GetMainEventId();
        var @event = await eventService.GetEvent(eventId);
        
        TravellingPostcards = await userService.GetTravellingPostcards(us!, @event!);
        MaxTravellingPostcards = _postcardConfig.MaxTravellingPostcards;
        CanUserSendPostcards = TravellingPostcards < MaxTravellingPostcards;
        
        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        var us = await userManager.GetUserAsync(User);
        var eventId = await mainEventService.GetMainEventId();
        var @event = await eventService.GetEvent(eventId);

        Entities.Postcard? postcard = null;

        try
        {
            postcard = await postcardService.SendPostcard(us!, @event!);
        }
        catch (CPCXException e)
        {
            // TODO - Set status message
            return Page();
        }

        return RedirectToPage("/Postcard/Index", new { postcardId = postcard.FullPostCardId });
    }
}