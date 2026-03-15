// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System.ComponentModel.DataAnnotations;
using cpcx.Config;
using cpcx.Entities;
using cpcx.Models;
using cpcx.Services;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace cpcx.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<CpcxUser> _signInManager;
        private readonly UserManager<CpcxUser> _userManager;
        private readonly IUserStore<CpcxUser> _userStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly MainEventService _mainEventService;
        private readonly IEventService _eventService;
        private readonly IAvatarService _avatarService;
        private readonly CpcxConfig _cpcxConfig;

        public RegisterModel(
            UserManager<CpcxUser> userManager,
            IUserStore<CpcxUser> userStore,
            SignInManager<CpcxUser> signInManager,
            ILogger<RegisterModel> logger,
            MainEventService mainEventService,
            IEventService eventService,
            IAvatarService avatarService,
            IOptions<CpcxConfig> cpcxConfig)
        {
            _userManager = userManager;
            _userStore = userStore;
            _signInManager = signInManager;
            _logger = logger;
            _mainEventService = mainEventService;
            _eventService = eventService;
            _avatarService = avatarService;
            _cpcxConfig = cpcxConfig.Value;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public class InputModel
        {
            [Required]
            [RegularExpression(@"^[abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789._-]{3,40}$",
                ErrorMessage = "Username must be 3–40 characters and may only contain letters, digits, '.', '_', or '-'.")]
            [Display(Name = "Username")]
            public string Username { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(string returnUrl = null)
        {
            if (!_cpcxConfig.EnableRegistration)
            {
                TempData["StatusMessage"] = $"{StatusMessageType.Info}%Registrations for new users are currently closed.";
                return RedirectToPage("/Index", new { area = "" });
            }

            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            if (!_cpcxConfig.EnableRegistration)
            {
                TempData["StatusMessage"] = $"{StatusMessageType.Info}%Registrations for new users are currently closed.";
                return RedirectToPage("/Index", new { area = "" });
            }

            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            if (ModelState.IsValid)
            {
                var user = CreateUser();

                await _userStore.SetUserNameAsync(user, Input.Username, CancellationToken.None);
                var avatars = _avatarService.GetAvatarListForUser(user);
                if (avatars.Count > 0)
                    user.AvatarPath = avatars[Random.Shared.Next(avatars.Count)];
                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User created a new account with password.");

                    var mainEventId = await _mainEventService.GetMainEventId();
                    await _eventService.AddUser(mainEventId, user, "");

                    await _signInManager.SignInAsync(user, isPersistent: false);
                    var publicId = await _mainEventService.GetMainEventPublicId();
                    TempData["StatusMessage"] = $"{StatusMessageType.Info}%Please fill in your address as soon as possible so we know where to send your postcards!";
                    return RedirectToPage("/Event/Index", new { area = "", eventPublicId = publicId });
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return Page();
        }

        private CpcxUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<CpcxUser>();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(User)}'. " +
                    $"Ensure that '{nameof(User)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                    $"override the register page in /Areas/Identity/Pages/Account/Register.cshtml");
            }
        }
    }
}
