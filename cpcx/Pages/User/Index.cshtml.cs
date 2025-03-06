using cpcx.Entities;
using cpcx.Models;
using cpcx.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace cpcx.Pages.User;

public class Index(UserManager<CpcxUser> userManager, IEventService eventService, MainEventService mainEventService) : MessagePageModel
{
    public CpcxUser UserProfile { get; set; }
    
    public EventUser ProfileStats { get; set; }
    
    public async Task<IActionResult> OnGet(string alias)
    {
        var mainEventId = await mainEventService.GetMainEventId();
        var us = await userManager.FindByNameAsync(alias);

        if (us == null)
        {
            SetStatusMessage($"User {alias} not found", StatusMessageType.Info);
            return RedirectToPage("/Index");
        }

        var eu = await eventService.GetEventUser(mainEventId, us);

        UserProfile = us;
        ProfileStats = eu;

        return Page();
    }
}