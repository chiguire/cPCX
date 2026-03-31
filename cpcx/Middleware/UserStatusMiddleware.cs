using cpcx.Entities;
using Microsoft.AspNetCore.Identity;

namespace cpcx.Middleware;

public class UserStatusMiddleware(RequestDelegate next, TimeProvider timeProvider)
{
    private static readonly string[] SkippedPaths =
    [
        "/Restricted",
        "/Identity/Account/Logout",
    ];

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true &&
            !SkippedPaths.Any(p => context.Request.Path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase)))
        {
            var userManager = context.RequestServices.GetRequiredService<UserManager<CpcxUser>>();
            var user = await userManager.GetUserAsync(context.User);

            if (user != null)
            {
                var now = timeProvider.GetUtcNow().UtcDateTime;
                if (user.DeactivatedDate != DateTime.UnixEpoch || user.BlockedUntilDate > now)
                {
                    context.Response.Redirect("/Restricted");
                    return;
                }
            }
        }

        await next(context);
    }
}
