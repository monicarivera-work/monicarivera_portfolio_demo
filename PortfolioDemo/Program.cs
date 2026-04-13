using PortfolioDemo.Services;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IUnansweredQuestionService, UnansweredQuestionService>();

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
    // Allows: local resources, Bootstrap/FA from CDN, Google Fonts, Azure App Insights
    headers["Content-Security-Policy"] =
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' https://js.monitor.azure.com; " +
        "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com https://cdnjs.cloudflare.com; " +
        "font-src 'self' https://fonts.gstatic.com https://cdnjs.cloudflare.com; " +
        "img-src 'self' data:; " +
        "connect-src 'self' https://*.monitor.azure.com https://*.applicationinsights.azure.com https://*.in.applicationinsights.azure.com; " +
        "frame-ancestors 'none'; " +
        "form-action 'self'; " +
        "base-uri 'self';";

    await next();
});

app.UseHttpLogging();
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseRateLimiter();

app.UseAuthorization();

app.MapControllers();
app.MapRazorPages();

app.Run();
