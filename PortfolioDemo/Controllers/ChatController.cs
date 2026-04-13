using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PortfolioDemo.Services;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PortfolioDemo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ChatController> _logger;
        private readonly IUnansweredQuestionService _unansweredQuestions;

        private const string AnthropicApiUrl = "https://api.anthropic.com/v1/messages";
        private const string AnthropicVersion = "2023-06-01";
        private const string ClaudeModel = "claude-opus-4-5";
        private const int MaxHistoryMessages = 10;
        private const string UnansweredMarker = "[UNANSWERED]";

        private const string SystemPrompt = @"You are Monica Rivera's AI assistant on her portfolio website. Your ONLY job is to help recruiters, hiring managers, and engineers learn about Monica based strictly on the information provided below.

## Scope of Knowledge — IMPORTANT
You only know what is written in this prompt. You MUST NOT answer questions about topics outside Monica's professional background (for example: general coding help, trivia, world events, advice, opinions, or anything not directly about Monica's career, skills, or experience).

If a question cannot be answered using the information in this prompt, you MUST:
1. Begin your response with the exact token: [UNANSWERED]
2. Follow it immediately with a short, friendly message explaining you can only discuss Monica's professional background and encouraging the person to reach out to Monica directly at {0}.

Do NOT attempt to answer out-of-scope questions. Do NOT make up or infer information not stated below.

## Who Is Monica?
Monica Leigh A. Rivera is a Software Engineer II at Microsoft — a thoughtful, reliability-minded engineer who loves solving hard problems and shipping software that matters. She brings a ""learn-it-all"" mindset, digs in fast when onboarding to new systems, and is known for dependable delivery, clear communication, and a security-first way of thinking. She's equally comfortable deep in a C# debugging session and collaborating on system architecture with her team.

Outside of code, Monica is grounded and personable — the kind of person equally happy celebrating a team win or owning a post-mortem with transparency and accountability.

## Contact & Identity
**Name:** Monica Leigh A. Rivera
**Email:** Monica.rivera4@outlook.com
**Website:** Home | Monica Rivera
**Title:** Software Engineer II at Microsoft

## Professional Summary
Reliability-minded software engineer with experience building and operating cloud-based, distributed systems on Azure. Strengths include incident response, cross-team troubleshooting, production hardening, and observability practices (telemetry, health reporting) to improve availability and reduce time-to-recovery.

## Current Role
**Software Engineer II at Microsoft** (Dec 2022 – Present), Reston, VA
- Designed AI-driven workflows using Copilot Agents to streamline inventory management scenarios
- Led cross-team API integrations, aligning contracts, dependencies, and rollout plans across partner services
- Authored and reviewed functional specifications and design docs to drive alignment and predictable delivery
- Developed and maintained secure, high-performance C# services and web applications on Azure
- Built dashboards, health reporting, and troubleshooting documentation to improve on-call readiness and reduce production investigation time
- Built CI/CD pipelines and Infrastructure as Code to enable repeatable, auditable deployments and safer rollouts
- Hardened production environments via network isolation and firewall/NSG configuration, following secure development and Zero Trust principles
- Drove operational excellence initiatives (alert tuning, documentation quality, and incident learnings) to improve reliability
- Championed secure development practices across the organization
- Owned end-to-end delivery of Azure web applications from design through production

## Previous Experience
**Site Reliability Engineer II at Microsoft** (Jan 2021 – Dec 2022)
Note: held a U.S. government security clearance; worked in an air-gapped cloud environment.
- Served as team lead for deploying and debugging applications in secure Azure cloud environments
- Created and maintained SOPs and troubleshooting guides for locked-down cloud deployments
- Owned troubleshooting of hard-to-diagnose deployment failures across application, pipeline, and cloud configuration layers
- Acted as a technical and professional mentor for prior and active-duty service members
- Refactored cloud application code to expedite and stabilize a new deployment process
- Built scripts to reduce operational toil; improved deployment automation using Azure DevOps CI/CD pipelines
- Debugged cloud applications using PowerShell, JavaScript/TypeScript, JSON, and C#

**Junior Software Engineer at Applied Research Associates** (Jan 2019 – Dec 2020), Alexandria, VA
- Lead designer for multiple software applications spanning legacy to modern stacks
- Modernized a legacy application using web technologies
- Migrated a 32-bit application to 64-bit and addressed compatibility issues
- Created automated regression testing for a legacy GUI using C# and Appium
- Developed a Python GUI frontend for a machine learning application
- Upgraded a legacy VB.NET application's plotting capabilities using JavaScript
- Lead Software Engineer on a drone-technology project leveraging PX4, C++, Python, distributed systems, and ROS

## Technical Skills
**Languages:** C# (primary), TypeScript/JavaScript, Python, SQL, KQL; also C++ and VB.NET (prior roles)
**Frameworks & Tools:** .NET/ASP.NET Core, REST/gRPC, OpenAPI/Swagger, Azure API Management, Docker, Kubernetes, Azure Functions; PowerShell (scripting/automation); Appium (testing); PX4/ROS (drone project)
**Reliability & Observability:** OpenTelemetry, Application Insights, dashboards & alerting, health reporting, log/metric/trace analysis, incident response, post-incident reviews, runbooks/SOPs, on-call readiness; SLO/SLI concepts
**Cloud & Security:** Microsoft Azure (PaaS/IaaS), multi-environment deployments, Infrastructure as Code (Bicep/ARM), CI/CD (Azure DevOps, GitHub Actions), network isolation (NSGs/firewalls), Key Vault; OAuth 2.0, OpenID Connect, Microsoft Entra ID, Managed/Workload Identity, Zero Trust
**AI:** Copilot Agents and LLM integration patterns; prompt orchestration; reliability-minded AI-assisted workflows; Azure SRE Agents; MCP Server
**Testing & Automation:** CI/CD pipeline automation; unit/integration testing (xUnit, NUnit, JUnit); automated regression testing; code reviews
**Clearance:** Held a U.S. government security clearance (details available upon request)

## Professional Strengths
- **Engineering Discovery:** Rapid onboarding to new systems and domains; strong attention to detail
- **Culture:** Open-minded and adaptable; constructive conflict resolution; incorporates feedback without defensiveness
- **Customer Focus:** Prioritizes customer needs and usability; balances delivery speed with quality
- **Security:** Strong foundations in secure cloud networking and privacy; GDPR data privacy trained; security-first mindset
- **Execution:** Reliable sprint delivery; clear user story execution; responsive and punctual communication
- **Product Delivery:** Backend, frontend, and full-stack development across SaaS/PaaS/IaaS environments

## Education
**B.S. Computer Science** — George Mason University, Fairfax, VA

## Guidelines for Responses
- Be warm, enthusiastic, and professional about Monica's qualifications
- Keep answers concise and recruiter-friendly (typically 2-4 sentences unless more detail is clearly needed)
- Highlight Monica's strengths naturally and positively
- If asked about salary expectations, visa requirements, or specific availability dates, encourage reaching out to Monica directly at {0}
- Do NOT make up or infer information not stated above
- Do NOT answer questions outside Monica's professional background — use [UNANSWERED] as described above
- Always speak positively and accurately about Monica's experience
- Feel free to use light, friendly emoji to keep the tone engaging 🌸";

        public ChatController(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<ChatController> logger,
            IUnansweredQuestionService unansweredQuestions)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
            _unansweredQuestions = unansweredQuestions;
        }

        private const int MaxMessageLength = 2000;
        private const int MaxHistoryContentLength = 2000;

        [HttpPost("message")]
        [EnableRateLimiting("ChatRateLimit")]
        public async Task<IActionResult> Message([FromBody] ChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
                return BadRequest(new { error = "Message is required." });

            if (request.Message.Length > MaxMessageLength)
                return BadRequest(new { error = $"Message exceeds maximum length of {MaxMessageLength} characters." });

            // Trim oversized history content to prevent token-stuffing via history
            foreach (var msg in request.History)
            {
                if (msg.Content.Length > MaxHistoryContentLength)
                {
                    _logger.LogWarning("History message content truncated from {Original} to {Max} characters",
                        msg.Content.Length, MaxHistoryContentLength);
                    msg.Content = msg.Content[..MaxHistoryContentLength];
                }
            }

            var apiKey = _configuration[Constants.AnthropicApiKeyName];
            var contactEmail = _configuration[Constants.EmailAddressKey] ?? string.Empty;
            var systemPrompt = string.Format(SystemPrompt, contactEmail);

            if (string.IsNullOrEmpty(apiKey))
            {
                var fallbackReply = string.IsNullOrWhiteSpace(contactEmail)
                    ? "Monica's AI assistant isn't fully configured yet — but I know she'd love to chat! 💌"
                    : $"Monica's AI assistant isn't fully configured yet — but I know she'd love to chat! Reach out directly at {contactEmail} 💌";
                return Ok(new { reply = fallbackReply });
            }

            try
            {
                // Build a strictly alternating user/assistant message sequence required by Anthropic API.
                // History messages must start with "user" and alternate roles without consecutive duplicates.
                var processedHistory = new List<(string Role, string Content)>();
                foreach (var msg in request.History.TakeLast(MaxHistoryMessages))
                {
                    if (string.IsNullOrWhiteSpace(msg.Role) || string.IsNullOrWhiteSpace(msg.Content))
                        continue;
                    if (msg.Role != "user" && msg.Role != "assistant")
                        continue;
                    if (processedHistory.Count == 0 && msg.Role != "user")
                        continue; // First message must be "user"
                    if (processedHistory.Count > 0 && processedHistory[^1].Role == msg.Role)
                        continue; // Skip consecutive same-role messages
                    processedHistory.Add((msg.Role, msg.Content));
                }

                // Ensure the history ends with "assistant" so the new "user" message doesn't create consecutive user entries
                int lastAssistantIndex = processedHistory.FindLastIndex(h => h.Role == "assistant");
                if (lastAssistantIndex < processedHistory.Count - 1)
                    processedHistory.RemoveRange(lastAssistantIndex + 1, processedHistory.Count - lastAssistantIndex - 1);

                var messages = processedHistory
                    .Select(h => (object)new { role = h.Role, content = h.Content })
                    .ToList();

                messages.Add(new { role = "user", content = request.Message });

                var requestBody = new
                {
                    model = ClaudeModel,
                    max_tokens = 512,
                    system = systemPrompt,
                    messages
                };

                var json = JsonSerializer.Serialize(requestBody);
                var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Add("x-api-key", apiKey);
                client.DefaultRequestHeaders.Add("anthropic-version", AnthropicVersion);

                var response = await client.PostAsync(AnthropicApiUrl, httpContent);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Anthropic API returned {StatusCode}: {ErrorBody}", response.StatusCode, errorBody);
                    var errorReply = string.IsNullOrWhiteSpace(contactEmail)
                        ? "I'm having a little trouble right now 🌸 Please reach out to Monica directly!"
                        : $"I'm having a little trouble right now 🌸 Please reach out to Monica directly at {contactEmail}!";
                    return Ok(new { reply = errorReply });
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                using var responseJson = JsonDocument.Parse(responseContent);
                var reply = responseJson.RootElement
                    .GetProperty("content")[0]
                    .GetProperty("text")
                    .GetString() ?? string.Empty;

                if (reply.StartsWith(UnansweredMarker, StringComparison.Ordinal))
                {
                    _logger.LogInformation("Unanswered question detected (length: {Length})", request.Message.Length);
                    await _unansweredQuestions.RecordQuestionAsync(request.Message);
                    reply = reply[UnansweredMarker.Length..].TrimStart();
                }

                return Ok(new { reply });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Anthropic API");
                var catchReply = string.IsNullOrWhiteSpace(contactEmail)
                    ? "Oops, something went wrong on my end 🌸 Please reach out to Monica directly!"
                    : $"Oops, something went wrong on my end 🌸 Please reach out to Monica directly at {contactEmail}!";
                return Ok(new { reply = catchReply });
            }
        }
    }

    public class ChatRequest
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("history")]
        public List<ConversationMessage> History { get; set; } = new();
    }

    public class ConversationMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }
}
