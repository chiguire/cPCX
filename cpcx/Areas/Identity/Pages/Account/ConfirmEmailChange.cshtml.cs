// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using cpcx.Entities;
using cpcx.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace cpcx.Areas.Identity.Pages.Account
{
    public class ConfirmEmailChangeModel : MessagePageModel
    {
        private readonly UserManager<CpcxUser> _userManager;
        private readonly SignInManager<CpcxUser> _signInManager;
        private readonly ILogger<ConfirmEmailChangeModel> _logger;

        public ConfirmEmailChangeModel(UserManager<CpcxUser> userManager, SignInManager<CpcxUser> signInManager, ILogger<ConfirmEmailChangeModel> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        public async Task<IActionResult> OnGetAsync(string userId, string email, string code)
        {
            if (userId == null || email == null || code == null)
            {
                return RedirectToPage("/Index");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{userId}'.");
            }

            code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
            var result = await _userManager.ChangeEmailAsync(user, email, code);
            if (!result.Succeeded)
            {
                _logger.LogError($"Error changing email for '{userId}': {string.Join(',', result.Errors.Select(e => e.Description))}");
                SetStatusMessage("Error changing email.", StatusMessageType.Error);
                return RedirectToPage("/Account/Manage/Email", new { area = "Identity" });
            }

            await _signInManager.RefreshSignInAsync(user);
            SetStatusMessage("Thank you for confirming your email change.", StatusMessageType.Success);
            return RedirectToPage("/Account/Manage/Email", new { area = "Identity" });
        }
    }
}
