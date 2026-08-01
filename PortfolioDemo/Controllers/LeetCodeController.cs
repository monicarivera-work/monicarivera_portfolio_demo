using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PortfolioDemo.Services;

namespace PortfolioDemo.Controllers
{
    [ApiController]
    [Route("api/leetcode")]
    public class LeetCodeController : ControllerBase
    {
        private readonly ILeetCodeService _leetCodeService;

        public LeetCodeController(ILeetCodeService leetCodeService)
        {
            _leetCodeService = leetCodeService;
        }

        [HttpGet("stats")]
        [EnableRateLimiting("LeetCodeRateLimit")]
        public async Task<IActionResult> GetStats()
        {
            var stats = await _leetCodeService.GetUserStatsAsync(Constants.LeetCodeUsername);
            if (stats == null)
                return StatusCode(503, new { error = "Unable to reach LeetCode at this time." });
            return Ok(stats);
        }
    }
}
