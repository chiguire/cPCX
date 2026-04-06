using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using cpcx.Config;
using cpcx.Entities;
using cpcx.Models;
using cpcx.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace cpcx.Pages.Onboarding;

[Authorize]
public class Index(
    UserManager<CpcxUser> userManager,
    IUserService userService,
    IAvatarService avatarService,
    MainEventService mainEventService,
    IEmailSender emailSender,
    IOptions<SmtpConfig> smtpConfig) : MessagePageModel
{
    [BindProperty] public int Step { get; set; } = 1;
    [BindProperty] public bool Skip { get; set; } = false;
    [BindProperty] public ProfileInput Profile { get; set; } = new();
    [BindProperty] public AddressInput Address { get; set; } = new();
    [BindProperty] public AvatarInput Avatar { get; set; } = new();
    [BindProperty] public EmailStepInput EmailStep { get; set; } = new();

    public List<SelectListItem> Pronouns { get; set; } = [];
    public List<string> Avatars { get; set; } = [];

    public class ProfileInput
    {
        [Display(Name = "Pronouns")]
        public Pronoun Pronoun { get; set; }

        [Display(Name = "Profile Description")]
        [MaxLength(3000)]
        public string? ProfileDescription { get; set; }
    }

    public class AddressInput
    {
        [Display(Name = "Address in event")]
        public string? AddressInEvent { get; set; }

        [Display(Name = "Active in event")]
        public bool UserActiveInEvent { get; set; }
    }

    public class AvatarInput
    {
        public string? SelectedAvatar { get; set; }
    }

    public class EmailStepInput
    {
        [EmailAddress]
        [Display(Name = "Email (optional)")]
        public string? Email { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        Step = 1;
        await InitStepDataAsync(user);
        await LoadViewDataAsync(user);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        if (!Skip)
        {
            switch (Step)
            {
                case 1:
                    if (!ModelState.IsValid)
                    {
                        await LoadViewDataAsync(user);
                        return Page();
                    }
                    if (user.Pronouns != Profile.Pronoun)
                        await userService.SetUserPronoun(user, Profile.Pronoun);
                    var desc = Profile.ProfileDescription ?? "";
                    if (user.ProfileDescription != desc)
                        await userService.SetUserProfileDescription(user, desc);
                    break;

                case 2:
                    if (Address.UserActiveInEvent && string.IsNullOrWhiteSpace(Address.AddressInEvent))
                        ModelState.AddModelError("Address.AddressInEvent", "Address is required when active in event.");
                    if (!ModelState.IsValid)
                    {
                        await LoadViewDataAsync(user);
                        return Page();
                    }
                    var eventId = await mainEventService.GetMainEventId();
                    var eu = await userService.GetEventUser(eventId, user.Id);
                    if (eu.Address != (Address.AddressInEvent ?? ""))
                        await userService.SetUserAddress(user.Id, eventId, Address.AddressInEvent ?? "");
                    if (eu.ActiveInEvent != Address.UserActiveInEvent)
                        await userService.SetUserActiveInEvent(user.Id, eventId, Address.UserActiveInEvent);
                    break;

                case 3:
                    var allAvatars = avatarService.GetAvatarListForUser(user);
                    if (!string.IsNullOrEmpty(Avatar.SelectedAvatar) && allAvatars.Contains(Avatar.SelectedAvatar)
                        && user.AvatarPath != Avatar.SelectedAvatar)
                    {
                        await userService.SetUserAvatar(user, Avatar.SelectedAvatar);
                    }
                    break;

                case 4:
                    if (!ModelState.IsValid)
                    {
                        await LoadViewDataAsync(user);
                        return Page();
                    }
                    return await FinishAsync(user);
            }
        }
        else if (Step == 4)
        {
            return await FinishAsync(user);
        }

        Step++;
        user = (await userManager.GetUserAsync(User))!;
        await InitStepDataAsync(user);
        await LoadViewDataAsync(user);
        return Page();
    }

    private async Task<IActionResult> FinishAsync(CpcxUser user)
    {
        if (!string.IsNullOrWhiteSpace(EmailStep.Email))
        {
            var smtp = smtpConfig.Value;
            if (!string.IsNullOrEmpty(smtp.Host))
            {
                var userId = await userManager.GetUserIdAsync(user);
                var code = await userManager.GenerateChangeEmailTokenAsync(user, EmailStep.Email);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                var callbackUrl = Url.Page(
                    "/Account/ConfirmEmailChange",
                    pageHandler: null,
                    values: new { area = "Identity", userId, email = EmailStep.Email, code },
                    protocol: Request.Scheme);
                await emailSender.SendEmailAsync(
                    EmailStep.Email,
                    "Confirm your email address",
                    EmailTemplates.ConfirmEmail(HtmlEncoder.Default.Encode(callbackUrl!)));
                SetStatusMessage("Thanks for setting up your profile! A confirmation email is on its way.", StatusMessageType.Success);
            }
            else
            {
                await userManager.SetEmailAsync(user, EmailStep.Email);
                SetStatusMessage("Thanks for setting up your profile! Welcome to DeerPost.", StatusMessageType.Success);
            }
        }
        else
        {
            SetStatusMessage("Thanks for setting up your profile! Welcome to DeerPost.", StatusMessageType.Success);
        }

        return RedirectToPage("/Index");
    }

    private async Task InitStepDataAsync(CpcxUser user)
    {
        var eventId = await mainEventService.GetMainEventId();
        var eu = await userService.GetEventUser(eventId, user.Id);

        Profile = new ProfileInput { Pronoun = user.Pronouns, ProfileDescription = user.ProfileDescription };
        Address = new AddressInput { AddressInEvent = eu.Address, UserActiveInEvent = eu.ActiveInEvent };
        Avatar = new AvatarInput { SelectedAvatar = user.AvatarPath };
        EmailStep = new EmailStepInput { Email = await userManager.GetEmailAsync(user) };
    }

    private async Task LoadViewDataAsync(CpcxUser user)
    {
        Pronouns = [];
        foreach (Pronoun p in Enum.GetValues(typeof(Pronoun)))
        {
            Pronouns.Add(new SelectListItem
            {
                Value = p.ToString(),
                Text = $"{p} ({p.GetDescription()})",
                Selected = user.Pronouns == p
            });
        }
        Avatars = avatarService.GetAvatarListForUser(user);
    }
}
