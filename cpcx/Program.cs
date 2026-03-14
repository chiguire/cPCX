using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using cpcx.Data;
using cpcx.Entities;
using cpcx.Config;
using cpcx.Controllers;
using cpcx.Infrastructure;
using cpcx.Middleware;
using cpcx.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<CpcxConfig>(
    builder.Configuration.GetSection(CpcxConfig.Cpcx));
builder.Services.Configure<PostcardConfig>(
     builder.Configuration.GetSection(PostcardConfig.Postcard));
builder.Services.Configure<IpAllowlistConfig>(
    builder.Configuration.GetSection(IpAllowlistConfig.IpAllowlist));
var license = builder.Configuration["AutoMapper:License"];
builder.Services.AddAutoMapper(cfg => cfg.LicenseKey = license, typeof(Program));
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IPostcardService, PostcardService>();
builder.Services.AddSingleton<MainEventService>();
builder.Services.AddSingleton<IAvatarService, AvatarService>();

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services
    .AddDefaultIdentity<CpcxUser>(options => {
        options.SignIn.RequireConfirmedAccount = false;
        options.SignIn.RequireConfirmedPhoneNumber = false;
        options.SignIn.RequireConfirmedEmail = false;
        options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._";
        options.User.RequireUniqueEmail = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>();
var enableLoadTestApi = builder.Configuration.GetValue<bool>($"{CpcxConfig.Cpcx}:EnableLoadTestApi");
builder.Services.AddControllers(options =>
{
    if (!enableLoadTestApi)
    {
        options.Conventions.Add(new DisableControllerConvention(typeof(LoadTestController)));
    }
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("check-alias", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromSeconds(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AddPageRoute("/Postcard/Travelling", "/Postcard/Traveling");
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseMiddleware<IpAllowlistMiddleware>();

app.UseRouting();

app.UseRateLimiter();

app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();

app.Run();
