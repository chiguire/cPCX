using Azure;
using cpcx.Entities;
using cpcx.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace cpcx.Controllers;

[AllowAnonymous]
public class UserController(UserManager<CpcxUser> userManager) : Controller
{
    [HttpPost]
    public async Task<ActionResult<bool>> CheckUserAliasAsync([FromBody]string aliasCandidate)
    {
        return Ok((await userManager.FindByNameAsync(aliasCandidate)) is null);
    }
}