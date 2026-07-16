using System.ComponentModel.DataAnnotations;
using cpcx.Config;
using cpcx.Entities;
using cpcx.Models;
using cpcx.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace cpcx.Pages.User;

[Authorize]
public class Report(
    UserManager<CpcxUser> userManager,
    IUserService userService,
    IEmailSender emailSender,
    IOptions<CpcxConfig> cpcxConfig,
    ILogger<Report> logger) : MessagePageModel
{
    public string ReportedUserAlias { get; set; } = "";
    public bool HasEmail { get; set; }

    [Display(Name = "Description")]
    [Required]
    [BindProperty]
    public string? Description { get; set; }

    public async Task<IActionResult> OnGet(string alias)
    {
        var currentUser = await userManager.GetUserAsync(User);
        var reportedUser = await userManager.FindByNameAsync(alias);

        if (reportedUser == null || reportedUser.IsDeleted)
        {
            SetStatusMessage($"User {alias} not found", StatusMessageType.Info);
            return RedirectToPage("/Index");
        }

        if (reportedUser.Id == currentUser!.Id)
        {
            SetStatusMessage("You can't report your own profile.", StatusMessageType.Info);
            return RedirectToPage("/User/Index", new { alias });
        }

        ReportedUserAlias = reportedUser.UserName!;
        HasEmail = !string.IsNullOrEmpty(await userManager.GetEmailAsync(currentUser));

        return Page();
    }

    public async Task<IActionResult> OnPost(string alias)
    {
        var currentUser = await userManager.GetUserAsync(User);
        var reportedUser = await userManager.FindByNameAsync(alias);

        if (reportedUser == null || reportedUser.IsDeleted || reportedUser.Id == currentUser!.Id)
        {
            SetStatusMessage($"User {alias} not found", StatusMessageType.Info);
            return RedirectToPage("/Index");
        }

        var reporterEmail = await userManager.GetEmailAsync(currentUser);
        if (string.IsNullOrEmpty(reporterEmail))
        {
            SetStatusMessage("You need to set your email address before submitting this form.", StatusMessageType.Error);
            return RedirectToPage(new { alias });
        }

        if (string.IsNullOrWhiteSpace(Description))
        {
            SetStatusMessage("Please provide a description.", StatusMessageType.Error);
            return RedirectToPage(new { alias });
        }

        var caretakerEmail = cpcxConfig.Value.CaretakerEmail;
        if (string.IsNullOrEmpty(caretakerEmail))
        {
            logger.LogWarning("Caretaker email not configured — cannot send profile report from user {UserId}", currentUser.UserName);
            SetStatusMessage("Could not send the report — caretaker email is not configured.", StatusMessageType.Error);
            return RedirectToPage(new { alias });
        }

        var profileUrl = Url.Page("/User/Index", null, new { alias }, Request.Scheme);
        var guidelinesUrl = Url.Page("/Guidelines", null, null, Request.Scheme);

        var subject = $"Profile report — {alias} reported by {currentUser.UserName}";
        var body = $"""
            <p>User <strong>{currentUser.UserName}</strong> reported the profile of <strong>{reportedUser.UserName}</strong>.</p>
            <p><strong>Reported profile:</strong> <a href="{profileUrl}">{profileUrl}</a></p>
            <p><strong>Reply to:</strong> <a href="mailto:{reporterEmail}">{reporterEmail}</a></p>
            <p>Reviewed against the <a href="{guidelinesUrl}">DeerPost Community Guidelines</a>
                and the <a href="https://www.emfcamp.org/code-of-conduct">EMF Code of Conduct</a>.</p>
            <hr/>
            <p><strong>Message from reporter:</strong></p>
            <p>{System.Net.WebUtility.HtmlEncode(Description)}</p>
            <hr/>
            <p><strong>{reportedUser.UserName}'s profile description:</strong></p>
            <p>{System.Net.WebUtility.HtmlEncode(reportedUser.ProfileDescription)}</p>
            """;

        await emailSender.SendEmailAsync(caretakerEmail, subject, body);
        logger.LogInformation("User {UserId} reported profile {ReportedUserId}", currentUser.UserName, reportedUser.UserName);

        var alreadyBlocked = await userService.HasBlocked(currentUser.Id, reportedUser.Id);
        var message = alreadyBlocked
            ? "Your report has been sent. The caretaker will look into it."
            : "Your report has been sent. The caretaker will look into it. You can also block this user if you haven't already.";
        SetStatusMessage(message, StatusMessageType.Success);

        return RedirectToPage("/User/Index", new { alias });
    }
}
