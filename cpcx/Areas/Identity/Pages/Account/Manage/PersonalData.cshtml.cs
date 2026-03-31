// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using cpcx.Config;
using cpcx.Data;
using cpcx.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace cpcx.Areas.Identity.Pages.Account.Manage
{
    public class PersonalDataModel(
        UserManager<CpcxUser> userManager,
        SignInManager<CpcxUser> signInManager,
        ILogger<PersonalDataModel> logger,
        ApplicationDbContext dbContext,
        IOptionsSnapshot<PostcardConfig> postcardConfig,
        TimeProvider timeProvider) : PageModel
    {
        private const string RequiredPhrase = "Delete my account";

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required]
            public string ConfirmationPhrase { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{userManager.GetUserId(User)}'.");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostDownloadAsync()
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{userManager.GetUserId(User)}'.");
            }

            logger.LogInformation("User with ID '{UserId}' asked for their personal data.", userManager.GetUserId(User));

            var personalData = new Dictionary<string, object>();

            // Standard [PersonalData] properties via reflection
            var personalDataProps = typeof(CpcxUser).GetProperties()
                .Where(prop => Attribute.IsDefined(prop, typeof(PersonalDataAttribute)));
            foreach (var p in personalDataProps)
            {
                personalData.Add(p.Name, p.GetValue(user)?.ToString() ?? "");
            }

            // Logins
            var logins = await userManager.GetLoginsAsync(user);
            foreach (var l in logins)
            {
                personalData.Add($"external-login:{l.LoginProvider}", l.ProviderKey);
            }

            // Postcards
            var expiryThreshold = timeProvider.GetUtcNow().AddHours(-postcardConfig.Value.PostcardExpirationTimeInHours);

            var sentPostcards = await dbContext.Postcards
                .Include(p => p.Event)
                .Include(p => p.Sender)
                .Include(p => p.Receiver)
                .Where(p => p.Sender.Id == user.Id)
                .OrderBy(p => p.SentOn)
                .ToListAsync();

            personalData["SentPostcards"] = sentPostcards.Select(p => new
            {
                PostcardId = p.FullPostCardId,
                SenderUsername = p.Sender.UserName,
                ReceiverUsername = p.Receiver.UserName,
                SentOn = p.SentOn.ToString("o"),
                RegisteredOn = FormatRegisteredOn(p, expiryThreshold),
            }).ToList();

            var receivedPostcards = await dbContext.Postcards
                .Include(p => p.Event)
                .Include(p => p.Sender)
                .Include(p => p.Receiver)
                .Where(p => p.Receiver.Id == user.Id)
                .OrderBy(p => p.SentOn)
                .ToListAsync();

            personalData["ReceivedPostcards"] = receivedPostcards.Select(p => new
            {
                PostcardId = p.FullPostCardId,
                SenderUsername = p.Sender.UserName,
                ReceiverUsername = p.Receiver.UserName,
                SentOn = p.SentOn.ToString("o"),
                RegisteredOn = FormatRegisteredOn(p, expiryThreshold),
            }).ToList();

            // Blocked users
            var userWithBlocks = await dbContext.Users
                .Include(u => u.BlockedUsers)
                .FirstAsync(u => u.Id == user.Id);

            personalData["BlockedUsers"] = (userWithBlocks.BlockedUsers ?? [])
                .Select(u => u.UserName)
                .OrderBy(n => n)
                .ToList();

            return File(
                JsonSerializer.SerializeToUtf8Bytes(personalData, new JsonSerializerOptions { WriteIndented = true }),
                "application/json",
                "PersonalData.json");
        }

        public async Task<IActionResult> OnPostDeleteAsync()
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{userManager.GetUserId(User)}'.");
            }

            if (Input?.ConfirmationPhrase != RequiredPhrase)
            {
                ModelState.AddModelError(nameof(Input.ConfirmationPhrase),
                    $"Please type \"{RequiredPhrase}\" to confirm.");
                return Page();
            }

            var anonymizedName = $"deleted_{user.Id}";
            user.UserName = anonymizedName;
            user.NormalizedUserName = anonymizedName.ToUpperInvariant();
            user.Email = $"{anonymizedName}@deleted.invalid";
            user.NormalizedEmail = $"{anonymizedName}@deleted.invalid".ToUpperInvariant();
            user.PhoneNumber = null;
            user.ProfileDescription = "";
            user.AvatarPath = "";
            user.Pronouns = Pronoun.Empty;
            user.IsDeleted = true;
            await userManager.UpdateSecurityStampAsync(user);

            var result = await userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException("Unexpected error anonymizing user.");
            }

            var eventUsers = await dbContext.EventUsers.Where(eu => eu.UserId == user.Id).ToListAsync();
            foreach (var eu in eventUsers)
            {
                eu.Address = "";
                eu.ActiveInEvent = false;
            }

            // Auto-register any travelling postcards sent to this user
            var travellingToUser = await dbContext.Postcards
                .Where(p => p.Receiver.Id == user.Id &&
                            (p.ReceivedOn == null || p.ReceivedOn == DateTimeOffset.UnixEpoch))
                .ToListAsync();
            foreach (var p in travellingToUser)
            {
                p.ReceivedOn = timeProvider.GetUtcNow();
            }

            await dbContext.SaveChangesAsync();

            await signInManager.SignOutAsync();
            logger.LogInformation("User with ID '{UserId}' deleted their account.", user.Id);

            return Redirect("~/");
        }

        private static string FormatRegisteredOn(Postcard p, DateTimeOffset expiryThreshold)
        {
            if (p.ReceivedOn == null || p.ReceivedOn == DateTimeOffset.UnixEpoch)
                return p.IsExpired(expiryThreshold) ? "expired" : "";
            return p.ReceivedOn.Value.ToString("o");
        }
    }
}
