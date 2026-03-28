using Microsoft.AspNetCore.Authentication.Cookies;
using PortfolioDemo.Services.Bunion;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddControllers();
builder.Services.AddHttpClient();

// ── The Bunion: article storage (singleton — JSON file) ──────────
builder.Services.AddSingleton<ArticleService>();

// ── The Bunion: cookie authentication for admin ──────────────────
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath        = "/Bunion/Admin/Login";
        options.LogoutPath       = "/Bunion/Admin/Logout";
        options.AccessDeniedPath = "/Bunion/Admin/Login";
        options.Cookie.Name      = "BunionAdmin";
        options.Cookie.HttpOnly  = true;
        options.Cookie.SameSite  = SameSiteMode.Strict;
        options.ExpireTimeSpan   = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("BunionAdmin", policy =>
        policy.RequireClaim("bunion_admin", "true"));
});

// Add Application Insights telemetry
builder.Services.AddApplicationInsightsTelemetry();

// Configure HTTP request logging for traffic monitoring
builder.Services.AddHttpLogging(logging =>
{
    logging.LoggingFields = Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.RequestMethod
        | Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.RequestPath
        | Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.ResponseStatusCode
        | Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.Duration;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpLogging();
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapRazorPages();

app.Run();
