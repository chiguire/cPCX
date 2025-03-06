using System.ComponentModel.DataAnnotations;
using cpcx.Entities;
using cpcx.Exceptions;
using cpcx.Models;
using cpcx.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace cpcx.Pages.Postcard;

[Authorize]
public class Register(MainEventService mainEventService,
                      IPostcardService postcardService,
                      UserManager<CpcxUser> userManager,
                      ILogger<Register> logger) : MessagePageModel
{
    public List<SelectListItem> EventPublicIds { get; set; } = [];
    
    [Required] [BindProperty] public string EventId { get; set; }
    
    [Required] [BindProperty] public string PostcardId { get; set; }
    
    public async Task<IActionResult> OnGet()
    {
        var mainEventId = await mainEventService.GetMainEventId();
        var mainEventPublicId = await mainEventService.GetMainEventPublicId();

        EventPublicIds =
        [
            new SelectListItem { Value = mainEventId.ToString(), Text = mainEventPublicId },
        ];
        EventId = mainEventId.ToString();
        
        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        var u = await userManager.GetUserAsync(User);
        //var eventId = EventPublicId.ToGuid(); var mainEventPublicId = (await eventService.GetEvent(eventPublicId)).PublicId; 
        var mainEventPublicId = await mainEventService.GetMainEventPublicId(); // Ignoring the dropdown for now
        
        logger.LogInformation("User {UserId} tries registering postcard ID {EventId}-{PostcardId}", u!.Alias, mainEventPublicId, PostcardId);
        
        if (string.IsNullOrWhiteSpace(PostcardId) || !int.TryParse(PostcardId, out _))
        {
            SetStatusMessage("Please enter a valid postcard id, it must be digits only", StatusMessageType.Error);
            return await OnGet();
        }

        try
        {
            var p = await postcardService.RegisterPostcard(u, mainEventPublicId, PostcardId);
            
            SetStatusMessage("Postcard registered successfully! You will get a postcard in return now.", StatusMessageType.Success);
            return RedirectToPage("/Postcard/Index", new { postcardId = p.FullPostCardId });
        }
        catch (CPCXException e)
        {
            if (e.ErrorCode == CPCXErrorCode.PostcardNotFound)
            {
                SetStatusMessage("Postcard not found, please check the postcard ID again", StatusMessageType.Error);
                return await OnGet();
            }
            
            SetStatusMessage("Unknown error, contact admin", StatusMessageType.Error);
            return await OnGet();
        }
    }
}