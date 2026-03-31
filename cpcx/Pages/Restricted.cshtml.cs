using cpcx.Config;
using cpcx.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace cpcx.Pages;

public class RestrictedModel(
    UserManager<CpcxUser> userManager,
    SignInManager<CpcxUser> signInManager,
    IOptions<CpcxConfig> cpcxConfig,
    TimeProvider timeProvider) : PageModel
{
    public bool IsDeactivated { get; private set; }
    public bool IsBlocked { get; private set; }
    public DateTime? BlockedUntil { get; private set; }
    public string CaretakerEmail => cpcxConfig.Value.CaretakerEmail;

    public async Task<IActionResult> OnGetAsync()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return RedirectToPage("/Index");
        }

        var user = await userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToPage("/Index");
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        IsDeactivated = user.DeactivatedDate != DateTime.UnixEpoch;
        IsBlocked = user.BlockedUntilDate > now;
        BlockedUntil = IsBlocked && user.BlockedUntilDate != DateTime.MaxValue
            ? user.BlockedUntilDate
            : null;

        if (!IsDeactivated && !IsBlocked)
        {
            return RedirectToPage("/Index");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostSignOutAsync()
    {
        await signInManager.SignOutAsync();
        return RedirectToPage("/Index");
    }
}