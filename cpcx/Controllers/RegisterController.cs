using cpcx.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace cpcx.Controllers;

[AllowAnonymous]
[Route("api/[controller]")]
public class RegisterController(UserManager<CpcxUser> userManager) : Controller
{
    [HttpPost("[action]")]
    [EnableRateLimiting("check-alias")]
    public async Task<ActionResult<bool>> CheckUserAliasAsync([FromBody] string aliasCandidate)
    {
        return Ok((await userManager.FindByNameAsync(aliasCandidate)) is null);
    }
}
