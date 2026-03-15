using cpcx.Config;
using cpcx.Entities;
using cpcx.Models;
using cpcx.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace cpcx.Pages.User;

[Authorize]
public class Index(
    UserManager<CpcxUser> userManager,
    IUserService userService,
    IPostcardService postcardService,
    MainEventService mainEventService,
    IOptions<CpcxConfig> cpcxConfig) : MessagePageModel
{
    private readonly int _pageSize = cpcxConfig.Value.PageSize;

    public CpcxUser UserProfile { get; set; } = null!;
    public EventUser ProfileStats { get; set; } = null!;
    public bool OwnProfile { get; set; } = false;
    public bool IsBlockedByMe { get; set; } = false;

    public List<Entities.Postcard> SentPostcards { get; set; } = [];
    public int SentTotalCount { get; set; }
    public int SentPage { get; set; }

    public List<Entities.Postcard> ReceivedPostcards { get; set; } = [];
    public int ReceivedTotalCount { get; set; }
    public int ReceivedPage { get; set; }

    public int PageSize => _pageSize;
    public int SentTotalPages => (int)Math.Ceiling((double)SentTotalCount / _pageSize);
    public int ReceivedTotalPages => (int)Math.Ceiling((double)ReceivedTotalCount / _pageSize);

    public async Task<IActionResult> OnGet(string alias, int sentPage = 1, int receivedPage = 1)
    {
        var mainEventId = await mainEventService.GetMainEventId();
        var us = await userManager.FindByNameAsync(alias);

        if (us == null)
        {
            SetStatusMessage($"User {alias} not found", StatusMessageType.Info);
            return RedirectToPage("/Index");
        }

        var currentUser = (await userManager.GetUserAsync(User))!;

        // If the profile owner has blocked the current viewer, deny access
        if (us.Id != currentUser.Id && await userService.HasBlocked(us.Id, currentUser.Id))
        {
            SetStatusMessage($"You cannot access {us.UserName}'s profile.", StatusMessageType.Error);
            return RedirectToPage("/Index");
        }

        var eu = await userService.GetEventUser(mainEventId, us.Id);

        UserProfile = us;
        ProfileStats = eu;
        OwnProfile = currentUser.Id == us.Id;
        IsBlockedByMe = !OwnProfile && await userService.HasBlocked(currentUser.Id, us.Id);

        SentPage = Math.Max(1, sentPage);
        ReceivedPage = Math.Max(1, receivedPage);

        (SentPostcards, SentTotalCount) = await postcardService.GetSentPostcards(us.Id, mainEventId, SentPage, _pageSize);
        (ReceivedPostcards, ReceivedTotalCount) = await postcardService.GetReceivedPostcards(us.Id, mainEventId, ReceivedPage, _pageSize);

        return Page();
    }

    public async Task<IActionResult> OnPostBlock(string alias)
    {
        var profileUser = await userManager.FindByNameAsync(alias);
        var currentUser = await userManager.GetUserAsync(User);
        if (profileUser == null || currentUser == null || profileUser.Id == currentUser.Id)
            return RedirectToPage(new { alias });

        await userService.BlockUser(currentUser.Id, profileUser.Id);
        return RedirectToPage(new { alias });
    }

    public async Task<IActionResult> OnPostUnblock(string alias)
    {
        var profileUser = await userManager.FindByNameAsync(alias);
        var currentUser = await userManager.GetUserAsync(User);
        if (profileUser == null || currentUser == null)
            return RedirectToPage(new { alias });

        await userService.UnblockUser(currentUser.Id, profileUser.Id);
        return RedirectToPage(new { alias });
    }
}
