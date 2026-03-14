using cpcx.Entities;
using cpcx.Exceptions;
using cpcx.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace cpcx.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LoadTestController(
    SignInManager<CpcxUser> signInManager,
    UserManager<CpcxUser> userManager,
    IPostcardService postcardService,
    IEventService eventService,
    IUserService userService,
    MainEventService mainEventService,
    ILogger<LoadTestController> logger) : ControllerBase
{
    public record LoginRequest(string Username, string Password);
    public record RegisterPostcardRequest(string PostcardId);

    /// <summary>
    /// Log in to the application. On success, a session cookie is returned which
    /// must be included in subsequent requests.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await signInManager.PasswordSignInAsync(
            request.Username, request.Password,
            isPersistent: true, lockoutOnFailure: false);

        if (!result.Succeeded)
        {
            logger.LogWarning("API login failed for user {Username}", request.Username);
            return Unauthorized(new { success = false });
        }

        logger.LogInformation("API login succeeded for user {Username}", request.Username);
        return Ok(new { success = true });
    }

    /// <summary>
    /// Send a postcard. Returns the postcard ID and the username of the recipient.
    /// </summary>
    [Authorize]
    [HttpPost("postcard/send")]
    public async Task<IActionResult> SendPostcard()
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var eventId = await mainEventService.GetMainEventId();
        var @event = await eventService.GetEvent(eventId);
        if (@event == null) return Problem("Active event not found");

        try
        {
            var postcard = await postcardService.SendPostcard(user, @event);
            return Ok(new
            {
                postcardId = postcard.FullPostCardId,
                receiverUsername = postcard.Receiver.UserName,
            });
        }
        catch (CPCXException e)
        {
            logger.LogWarning("API send postcard failed for user {Username}: {Error}", user.UserName, e.ErrorCode);
            return BadRequest(new { success = false, error = CPCXException.ErrorCodeMessage(e.ErrorCode) });
        }
    }

    /// <summary>
    /// Returns the number of postcards sent and received by the authenticated user in the active event.
    /// </summary>
    [Authorize]
    [HttpGet("postcard/stats")]
    public async Task<IActionResult> GetPostcardStats()
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var eventId = await mainEventService.GetMainEventId();

        try
        {
            var eu = await userService.GetEventUser(eventId, user.Id);
            var travelling = await postcardService.GetTravellingPostcards(user.Id, eventId, includeExpired: false);
            return Ok(new { postcardsSent = eu.PostcardsSent, postcardsReceived = eu.PostcardsReceived, postcardsTravelling = travelling.Count });
        }
        catch (CPCXException e)
        {
            logger.LogWarning("API postcard stats failed for user {Username}: {Error}", user.UserName, e.ErrorCode);
            return BadRequest(new { error = CPCXException.ErrorCodeMessage(e.ErrorCode) });
        }
    }

    /// <summary>
    /// Register a postcard. The postcardId is the numeric part only (e.g. "1234", not "E26-1234").
    /// Returns whether the postcard was successfully registered.
    /// </summary>
    [Authorize]
    [HttpPost("postcard/register")]
    public async Task<IActionResult> RegisterPostcard([FromBody] RegisterPostcardRequest request)
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.PostcardId) || !int.TryParse(request.PostcardId, out _))
            return BadRequest(new { success = false, error = "PostcardId must be digits only" });

        var mainEventPublicId = await mainEventService.GetMainEventPublicId();

        try
        {
            await postcardService.RegisterPostcard(user, mainEventPublicId, request.PostcardId);
            return Ok(new { success = true });
        }
        catch (CPCXException e)
        {
            logger.LogWarning("API register postcard failed for user {Username}: {Error}", user.UserName, e.ErrorCode);
            return BadRequest(new { success = false, error = CPCXException.ErrorCodeMessage(e.ErrorCode) });
        }
    }
}
