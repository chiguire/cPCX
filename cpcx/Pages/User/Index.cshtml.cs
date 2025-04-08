using cpcx.Entities;
using cpcx.Models;
using cpcx.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace cpcx.Pages.User;

[Authorize]
public class Index(UserManager<CpcxUser> userManager, IUserService userService, MainEventService mainEventService) : MessagePageModel
{
    public CpcxUser UserProfile { get; set; } = null!;
    
    public EventUser ProfileStats { get; set; } = null!;

    public bool OwnProfile { get; set; } = false;
    
    public async Task<IActionResult> OnGet(string alias)
    {
        var mainEventId = await mainEventService.GetMainEventId();
        var us = await userManager.FindByNameAsync(alias);

        if (us == null)
        {
            SetStatusMessage($"User {alias} not found", StatusMessageType.Info);
            return RedirectToPage("/Index");
        }

        var eu = await userService.GetEventUser(mainEventId, us.Id);

        UserProfile = us;
        ProfileStats = eu;
        OwnProfile = (await userManager.GetUserAsync(User))!.Id == us.Id;

        return Page();
    }
}