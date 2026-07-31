using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortfolioDemo.Models;
using PortfolioDemo.Services;
using System.Security.Claims;

namespace PortfolioDemo.Controllers
{
    [ApiController]
    [Route("api/sessions")]
    [Authorize]
    public class SessionController : ControllerBase
    {
        private readonly ISessionService _sessionService;

        public SessionController(ISessionService sessionService)
        {
            _sessionService = sessionService;
        }

        private string GetUserOid() =>
            User.FindFirstValue("oid") ??
            User.FindFirstValue("http://schemas.microsoft.com/identity/claims/objectidentifier") ??
            User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            throw new UnauthorizedAccessException("User OID not found in token.");

        [HttpGet]
        public async Task<IActionResult> ListSessions()
        {
            var sessions = await _sessionService.GetSessionsAsync(GetUserOid());
            return Ok(sessions);
        }

        [HttpGet("{sessionName}")]
        public async Task<IActionResult> GetSession(string sessionName)
        {
            var session = await _sessionService.GetSessionAsync(GetUserOid(), sessionName);
            if (session == null) return NotFound();
            return Ok(session);
        }

        [HttpPost]
        public async Task<IActionResult> SaveSession([FromBody] SaveSessionRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.SessionName))
                return BadRequest(new { error = "Session name is required." });
            if (request.SessionName.Length > 100)
                return BadRequest(new { error = "Session name must be 100 characters or fewer." });
            if (string.IsNullOrWhiteSpace(request.Code))
                return BadRequest(new { error = "Code cannot be empty." });

            await _sessionService.SaveSessionAsync(GetUserOid(), request);
            return Ok(new { message = "Session saved." });
        }

        [HttpDelete("{sessionName}")]
        public async Task<IActionResult> DeleteSession(string sessionName)
        {
            await _sessionService.DeleteSessionAsync(GetUserOid(), sessionName);
            return Ok(new { message = "Session deleted." });
        }
    }
}
