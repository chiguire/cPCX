using Azure;
using cpcx.Entities;
using cpcx.Services;
using Microsoft.AspNetCore.Mvc;

namespace cpcx.Controllers;

public class UserController(IUserService userService)
{
    [HttpPost]
    public async Task<bool> CheckUserAliasAsync([FromBody]string aliasCandidate)
    {
        return true;
    }
}