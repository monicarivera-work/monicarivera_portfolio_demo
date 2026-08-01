using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using PortfolioDemo.Services;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// ── Authentication: Microsoft Entra ID (Azure AD) ──────────────────────────
// Requires AzureAd:TenantId and AzureAd:ClientId in configuration.
// If the section is absent the app still starts; sessions require login to work.
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

builder.Services.AddAuthorization();

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IUnansweredQuestionService, UnansweredQuestionService>();
builder.Services.AddSingleton<ISessionService, SessionService>();
builder.Services.AddScoped<ILeetCodeService, LeetCodeService>();

// Named HTTP client for LeetCode with appropriate headers
builder.Services.AddHttpClient("LeetCode", client =>
{
    client.DefaultRequestHeaders.Add("User-Agent",
        "Mozilla/5.0 (compatible; PortfolioDemo/1.0; +https://monicarivera-portfolio-demo.azurewebsites.net)");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.Timeout = TimeSpan.FromSeconds(10);
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

// Rate limiting: protect the AI chat and file download endpoints from abuse
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Chat endpoint: max 10 requests per minute per IP address
    options.AddPolicy("ChatRateLimit", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    // File download endpoint: max 5 requests per minute per IP address
    options.AddPolicy("FileDownloadRateLimit", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    // LeetCode stats endpoint: max 10 requests per minute per IP address
    options.AddPolicy("LeetCodeRateLimit", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));
});

// Remove the Server header from Kestrel responses
builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

// Configure HSTS: 1 year max-age, include subdomains, eligible for preload list
builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);
    options.IncludeSubDomains = true;
    options.Preload = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // HSTS: 1-year max-age, include subdomains, preload-eligible (configured via AddHsts above)
    app.UseHsts();
}

// Add security response headers to every response
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;

    // Prevent MIME-type sniffing
    headers["X-Content-Type-Options"] = "nosniff";

    // Prevent the site from being embedded in frames (clickjacking protection)
    headers["X-Frame-Options"] = "DENY";

    // Control referrer information sent with requests
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

    // Restrict browser features
    headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";

    // Content Security Policy
    // Allows: local resources, Bootstrap/FA from CDN, Google Fonts, Azure App Insights,
    //         Chart.js from jsDelivr (LeetCode page), LeetCode avatar images,
    //         sandboxed iframes with blob/data srcdoc (CodeLab visualizer)
    headers["Content-Security-Policy"] =
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' https://js.monitor.azure.com https://cdn.jsdelivr.net; " +
        "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com https://cdnjs.cloudflare.com; " +
        "font-src 'self' https://fonts.gstatic.com https://cdnjs.cloudflare.com; " +
        "img-src 'self' data: https://assets.leetcode.com https://s3-us-west-1.amazonaws.com; " +
        "connect-src 'self' https://*.monitor.azure.com https://*.applicationinsights.azure.com https://*.in.applicationinsights.azure.com https://login.microsoftonline.com; " +
        "frame-src 'self'; " +
        "frame-ancestors 'none'; " +
        "form-action 'self' https://login.microsoftonline.com; " +
        "base-uri 'self';";

    await next();
});

app.UseHttpLogging();
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapRazorPages();

app.Run();
