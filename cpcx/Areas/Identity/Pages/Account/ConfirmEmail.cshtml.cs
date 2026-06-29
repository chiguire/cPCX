// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using cpcx.Entities;
using cpcx.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace cpcx.Areas.Identity.Pages.Account
{
    public class ConfirmEmailModel : MessagePageModel
    {
        private readonly UserManager<CpcxUser> _userManager;

        public ConfirmEmailModel(UserManager<CpcxUser> userManager)
        {
            _userManager = userManager;
        }
        public async Task<IActionResult> OnGetAsync(string userId, string code)
        {
            if (userId == null || code == null)
            {
                return RedirectToPage("/Index");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{userId}'.");
            }

            code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
            var result = await _userManager.ConfirmEmailAsync(user, code);
            if (result.Succeeded)
            {
                SetStatusMessage("Thank you for confirming your email.", StatusMessageType.Success);
            }
            else
            {
                SetStatusMessage("Error confirming your email.", StatusMessageType.Error);
            }
            return Page();
        }
    }
}
