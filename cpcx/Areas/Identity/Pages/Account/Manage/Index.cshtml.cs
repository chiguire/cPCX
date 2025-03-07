// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using cpcx.Entities;
using cpcx.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace cpcx.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel(
        UserManager<CpcxUser> userManager,
        SignInManager<CpcxUser> signInManager,
        IUserService userService,
        MainEventService mainEventService)
        : PageModel
    {
        public string Username { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        public List<SelectListItem> Pronouns { get; set; }

        public class InputModel
        {
            [Display(Name = "Pronouns")]
            public Pronoun Pronoun { get; set; }
            [Display(Name = "Active in EMF")]
            public bool ActiveInEmf { get; set; }
            
            [Display(Name = "Address in EMF")]
            [Required(ErrorMessage = "Address is required")]
            public string Address { get; set; }
            
            [Display(Name = "Profile Description")]
            [MaxLength(3000)]
            [Required(ErrorMessage = "Profile Description is required")]
            public string ProfileDescription { get; set; }
        }

        private async Task LoadAsync(CpcxUser user)
        {
            var userName = await userManager.GetUserNameAsync(user);
            var mainEventId = await mainEventService.GetMainEventId();
            var eventUser = await userService.GetEventUser(mainEventId, user.Id);

            Username = userName;

            Pronouns = [];

            foreach (var pronoun in Enum.GetValues(typeof(Pronoun)))
            {
                var p = (Pronoun)pronoun;
                Pronouns.Add(new SelectListItem
                {
                    Value = p.ToString(), 
                    Text = $"{p.ToString()} ({p.GetDescription()})", 
                    Selected = user.Pronouns == p
                });
            }
            
            Input = new InputModel
            {
                Pronoun = user.Pronouns,
                ActiveInEmf = eventUser.ActiveInEvent,
                Address = eventUser.Address,
                ProfileDescription = user.ProfileDescription,
            };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{userManager.GetUserId(User)}'.");
            }

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }
            
            var mainEventId = await mainEventService.GetMainEventId();
            var eventUser = await userService.GetEventUser(mainEventId, user.Id);

            if (user.Pronouns != Input.Pronoun)
            {
                await userService.SetUserPronoun(user, Input.Pronoun);
            }

            if (user.ProfileDescription != Input.ProfileDescription)
            {
                await userService.SetUserProfileDescription(user, Input.ProfileDescription);
            }

            if (eventUser.Address != Input.Address)
            {
                await userService.SetUserAddress(user.Id, mainEventId, Input.Address);
            }
            
            if (eventUser.ActiveInEvent != Input.ActiveInEmf)
            {
                await userService.SetUserActiveInEvent(user.Id, mainEventId, Input.ActiveInEmf);
            }
            
            await signInManager.RefreshSignInAsync(user);
            StatusMessage = "Your profile has been updated";
            return RedirectToPage();
        }
    }
}
