using cpcx.Entities;
using cpcx.Exceptions;
using cpcx.Models;
using cpcx.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace cpcx.Pages.Postcard;

[Authorize]
public class Index(UserManager<CpcxUser> userManager,
                   IPostcardService postcardService) : MessagePageModel
{
    public string PostcardId { get; set; } = null!;
    public Entities.Postcard Postcard { get; set; } = null!; 
    
    public async Task<IActionResult> OnGet(string postcardId)
    {
        var us = await userManager.GetUserAsync(User);
        
        try
        {
            var p = await postcardService.GetPostcard(postcardId);

            PostcardId = postcardId;
            Postcard = p;

            // If postcard has not been registered yet
            if (!p.ReceivedOn.HasValue || p.ReceivedOn.Value == DateTime.UnixEpoch)
            {
                // No idea why the receiver would look for the postcard ID before registering, but since we're there
                if (p.Receiver == us)
                {
                    return RedirectToPage("/Postcard/Register");
                }
                
                // Postcards aren't public until registered
                SetStatusMessage($"Postcard {postcardId} not found", FlashMessageType.Error);
                return RedirectToPage("/Index");
            }

            return Page();
        }
        catch (CPCXException e)
        {
            var msg = "";

            switch (e.ErrorCode)
            {
                case CPCXErrorCode.PostcardIdInvalidFormat:
                    msg = $"Postcard {postcardId} has an incorrect format";
                    break;
                case CPCXErrorCode.PostcardNotFound:
                    msg = $"Postcard {postcardId} not found";
                    break;
            }
            
            SetStatusMessage(msg, FlashMessageType.Error);
            return RedirectToPage("/Index");
        }
    }
}