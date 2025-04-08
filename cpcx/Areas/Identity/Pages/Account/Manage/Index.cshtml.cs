// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using cpcx.Entities;
using cpcx.Models;
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
        : MessagePageModel
    {

        [BindProperty]
        public InputModel Input { get; set; }

        public List<SelectListItem> Pronouns { get; set; }

        public class InputModel
        {
            [Display(Name = "Username")]
            public string Username { get; set; }
            
            [Display(Name = "Pronouns")]
            public Pronoun Pronoun { get; set; }

            [Display(Name = "Profile Description")]
            [MaxLength(3000)]
            [Required(ErrorMessage = "Profile Description is required")]
            public string ProfileDescription { get; set; }
        }

        private async Task LoadAsync(CpcxUser user)
        {
            var userName = await userManager.GetUserNameAsync(user);
            var mainEventId = await mainEventService.GetMainEventId();

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
                Username = userName,
                Pronoun = user.Pronouns,
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

            await signInManager.RefreshSignInAsync(user);
            StatusMessage = "Your profile has been updated";
            return RedirectToPage();
        }
    }
}
