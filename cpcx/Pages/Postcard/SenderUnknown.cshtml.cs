using System.ComponentModel.DataAnnotations;
using cpcx.Config;
using cpcx.Entities;
using cpcx.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace cpcx.Pages.Postcard;

[Authorize]
public class SenderUnknown(
    UserManager<CpcxUser> userManager,
    IEmailSender emailSender,
    IOptions<CpcxConfig> cpcxConfig,
    ILogger<SenderUnknown> logger) : MessagePageModel
{
    public bool HasEmail { get; set; }

    [Display(Name = "Description")]
    [Required]
    [BindProperty]
    public string? Description { get; set; }

    public async Task<IActionResult> OnGet()
    {
        var user = await userManager.GetUserAsync(User);
        HasEmail = !string.IsNullOrEmpty(await userManager.GetEmailAsync(user!));
        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        var user = await userManager.GetUserAsync(User);
        var userEmail = await userManager.GetEmailAsync(user!);

        if (string.IsNullOrEmpty(userEmail))
        {
            SetStatusMessage("You need to set your email address before submitting this form.", StatusMessageType.Error);
            return RedirectToPage();
        }

        if (string.IsNullOrWhiteSpace(Description))
        {
            SetStatusMessage("Please provide a description.", StatusMessageType.Error);
            return RedirectToPage();
        }

        var caretakerEmail = cpcxConfig.Value.CaretakerEmail;
        if (string.IsNullOrEmpty(caretakerEmail))
        {
            logger.LogWarning("Caretaker email not configured — cannot send unknown postcard report from user {UserId}", user!.UserName);
            SetStatusMessage("Could not send the report — caretaker email is not configured.", StatusMessageType.Error);
            return RedirectToPage();
        }

        var subject = $"Unknown postcard received — {user!.UserName}";
        var body = $"""
            <p>User <strong>{user.UserName}</strong> received a postcard they could not identify.</p>
            <p><strong>Reply to:</strong> <a href="mailto:{userEmail}">{userEmail}</a></p>
            <hr/>
            <p>{System.Net.WebUtility.HtmlEncode(Description)}</p>
            """;

        await emailSender.SendEmailAsync(caretakerEmail, subject, body);
        logger.LogInformation("User {UserId} submitted an unknown postcard report", user.UserName);

        SetStatusMessage("Your report has been sent. The caretaker will get back to you by email.", StatusMessageType.Success);
        return RedirectToPage();
    }
}
