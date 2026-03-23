namespace PortfolioDemo.Middleware
{
    public class VisitorLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<VisitorLoggingMiddleware> _logger;

        public VisitorLoggingMiddleware(RequestDelegate next, ILogger<VisitorLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var request = context.Request;
            var ip = request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim()
                     ?? context.Connection.RemoteIpAddress?.ToString()
                     ?? "unknown";
            var method = request.Method;
            var path = request.Path.Value ?? "/";
            var queryString = request.QueryString.HasValue ? request.QueryString.Value : string.Empty;
            var rawUserAgent = request.Headers.UserAgent.ToString();
            var userAgent = rawUserAgent.Length > 200 ? rawUserAgent[..200] : rawUserAgent;
            var referrer = request.Headers.Referer.ToString();

            _logger.LogInformation(
                "Visitor | IP: {IP} | Method: {Method} | Path: {Path}{QueryString} | Referrer: {Referrer} | UserAgent: {UserAgent}",
                ip, method, path, queryString, referrer, userAgent);

            await _next(context);
        }
    }
}
