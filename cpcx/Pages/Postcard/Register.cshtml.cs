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
    
    [Required] [BindProperty] public string? EventId { get; set; }
    
    [Display(Name = "Postcard ID:")]
    [Required] [BindProperty] public string? PostcardId { get; set; }
    
    [Display(Name = "Write a message to the sender:")]
    [Length(minimumLength: 0, maximumLength: 5000)]
    [BindProperty] public string? MessageToSender { get; set; }
    
    [Display(Name = "Receive a copy of the message")]
    [BindProperty] public bool ReceiverGetsCopyOfMessage { get; set; }
    
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
        
        logger.LogInformation("User {UserId} tries registering postcard ID {EventId}-{PostcardId}", u!.UserName, mainEventPublicId, PostcardId);
        
        if (string.IsNullOrWhiteSpace(PostcardId) || !int.TryParse(PostcardId, out _))
        {
            SetStatusMessage("Please enter a valid postcard id, it must be digits only", StatusMessageType.Error);
            return RedirectToPage();
        }

        try
        {
            var p = await postcardService.RegisterPostcard(u, mainEventPublicId, PostcardId);

            if (!string.IsNullOrWhiteSpace(MessageToSender))
            {
                // TODO Send message on email
                logger.LogInformation($"Message from {u.UserName} to sender {p.Sender.UserName} for postcard {mainEventPublicId}-{PostcardId} (Receives copy of message: {ReceiverGetsCopyOfMessage}): {MessageToSender}");
            }
            
            SetStatusMessage("Thanks for registering this postcard!", StatusMessageType.Success);
            return RedirectToPage("/Postcard/Index", new { postcardId = p.FullPostCardId });
        }
        catch (CPCXException e)
        {
            if (e.ErrorCode == CPCXErrorCode.PostcardNotFound)
            {
                SetStatusMessage("Postcard not found, please check the postcard ID again", StatusMessageType.Error);
                return RedirectToPage();
            }
            
            SetStatusMessage("Unknown error, contact admin", StatusMessageType.Error);
            return RedirectToPage();
        }
    }
}