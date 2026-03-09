#nullable disable

using cpcx.Entities;
using cpcx.Models;
using cpcx.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace cpcx.Areas.Identity.Pages.Account.Manage
{
    public class AvatarModel(
        UserManager<CpcxUser> userManager,
        SignInManager<CpcxUser> signInManager,
        IUserService userService,
        IAvatarService avatarService)
        : MessagePageModel
    {
        [BindProperty]
        public string SelectedAvatar { get; set; }

        public List<string> Avatars { get; set; }
        public string CurrentAvatar { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
                return NotFound($"Unable to load user with ID '{userManager.GetUserId(User)}'.");

            Avatars = avatarService.GetAvatarListForUser(user);
            CurrentAvatar = user.AvatarPath;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
                return NotFound($"Unable to load user with ID '{userManager.GetUserId(User)}'.");

            var avatars = avatarService.GetAvatarListForUser(user);

            if (!avatars.Contains(SelectedAvatar))
            {
                ModelState.AddModelError(string.Empty, "Invalid avatar selection.");
                Avatars = avatars;
                CurrentAvatar = user.AvatarPath;
                return Page();
            }

            if (user.AvatarPath != SelectedAvatar)
            {
                await userService.SetUserAvatar(user, SelectedAvatar);
            }

            await signInManager.RefreshSignInAsync(user);
            StatusMessage = "Your avatar has been updated";
            return RedirectToPage();
        }
    }
}