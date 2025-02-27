using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using cpcx.Data;
using cpcx.Entities;
using cpcx.Config;
using cpcx.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<CpcxConfig>(
    builder.Configuration.GetSection(CpcxConfig.Cpcx));
builder.Services.Configure<PostcardConfig>(
     builder.Configuration.GetSection(PostcardConfig.Postcard));

builder.Services.AddAutoMapper(typeof(Program));
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IPostcardService, PostcardService>();
builder.Services.AddSingleton<MainEventService>();

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services
    .AddDefaultIdentity<CpcxUser>(options => {
        options.SignIn.RequireConfirmedAccount = false;
        options.SignIn.RequireConfirmedPhoneNumber = false;
        options.SignIn.RequireConfirmedEmail = false;
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddRazorPages();

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

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();
