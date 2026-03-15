using cpcx.Entities;
using cpcx.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace cpcx.Areas.Identity.Pages.Account.Manage;

public class BlockedUsersModel(UserManager<CpcxUser> userManager, IUserService userService) : PageModel
{
    public List<CpcxUser> BlockedUsers { get; set; } = [];

    public async Task OnGetAsync()
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return;
        BlockedUsers = await userService.GetBlockedUsers(user.Id);
    }

    public async Task<IActionResult> OnPostUnblockAsync(Guid userId)
    {
        var user = await userManager.GetUserAsync(User);
        if (user != null)
            await userService.UnblockUser(user.Id, userId);
        return RedirectToPage();
    }
}
