using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace PortfolioDemo.Controllers
{
    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
    }

    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly AnthropicClient _anthropic;
        private readonly IMemoryCache _cache;
        private const int RateLimitRequests = 10;
        private static readonly TimeSpan RateLimitWindow = TimeSpan.FromMinutes(1);

        private const string SystemPrompt =
            "You are Monica Rivera, a Software Engineer II at Microsoft. " +
            "You are a cloud and AI expert who is passionate about Azure, .NET, and mentoring others. " +
            "You have extensive experience in cloud architecture, DevOps, and building intelligent applications. " +
            "Answer questions about your background, skills, and experience in a friendly and professional manner. " +
            "Keep responses concise and relevant to your professional portfolio.";

        public ChatController(AnthropicClient anthropic, IMemoryCache cache)
        {
            _anthropic = anthropic;
            _cache = cache;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] ChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Message))
                return BadRequest(new { error = "Message is required." });

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var cacheKey = $"ratelimit:{ip}";

            if (!_cache.TryGetValue(cacheKey, out int requestCount))
                requestCount = 0;

            if (requestCount >= RateLimitRequests)
                return StatusCode(429, new { error = "Too many requests. Please try again in a minute." });

            var options = new MemoryCacheEntryOptions().SetAbsoluteExpiration(RateLimitWindow);
            _cache.Set(cacheKey, requestCount + 1, options);

            try
            {
                var parameters = new MessageParameters
                {
                    Model = "claude-3-5-haiku-20241022",
                    MaxTokens = 300,
                    System = new List<SystemMessage> { new SystemMessage(SystemPrompt) },
                    Messages = new List<Message> { new Message(RoleType.User, request.Message) }
                };

                var response = await _anthropic.Messages.GetClaudeMessageAsync(parameters);
                return Ok(new { reply = response.Message.ToString() });
            }
            catch (Exception)
            {
                return StatusCode(503, new { error = "The AI service is currently unavailable. Please try again later." });
            }
        }
    }
}
