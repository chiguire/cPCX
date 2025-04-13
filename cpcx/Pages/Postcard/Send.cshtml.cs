using cpcx.Config;
using cpcx.Entities;
using cpcx.Exceptions;
using cpcx.Models;
using cpcx.Services;
using Humanizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace cpcx.Pages.Postcard;

[Authorize]
public class Send(MainEventService mainEventService,
                  UserManager<CpcxUser> userManager,
                  IEventService eventService, 
                  IOptionsSnapshot<PostcardConfig> postcardConfig,
                  IPostcardService postcardService) : MessagePageModel
{
    private readonly PostcardConfig _postcardConfig = postcardConfig.Value;
    
    [BindProperty]
    public bool NotesAcknowledged { get; set; }
    
    public int TravellingPostcards { get; set; }
    public int MaxTravellingPostcards { get; set; }
    public bool CanUserSendPostcards { get; set; }
    
    public async Task<IActionResult> OnGet()
    {
        var us = await userManager.GetUserAsync(User);
        var eventId = await mainEventService.GetMainEventId();
        var @event = await eventService.GetEvent(eventId);
        
        TravellingPostcards = (await postcardService.GetTravellingPostcards(us!.Id, @event!.Id)).Count;
        MaxTravellingPostcards = _postcardConfig.MaxTravellingPostcards;
        CanUserSendPostcards = TravellingPostcards < MaxTravellingPostcards;
        
        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        if (!NotesAcknowledged)
        {
            SetStatusMessage("You must acknowledge the message given before requesting an address", StatusMessageType.Warning);
            return RedirectToPage();
        }
        
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
            SetStatusMessage($"There was an error when trying to create a postcard: {CPCXException.ErrorCodeMessage(e.ErrorCode)}, please contact the administrator", StatusMessageType.Error);
            return RedirectToPage("/Index");
        }

        SetStatusMessage("You're sending a postcard! Please write the postcard ID separately from the address", StatusMessageType.Success);
        
        return RedirectToPage("/Postcard/Index", new { postcardId = postcard.FullPostCardId });
    }
}