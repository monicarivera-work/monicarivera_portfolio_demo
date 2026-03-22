using Microsoft.AspNetCore.Mvc;
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

        private const string AnthropicApiUrl = "https://api.anthropic.com/v1/messages";
        private const string AnthropicVersion = "2023-06-01";
        private const string ClaudeModel = "claude-opus-4-5";
        private const int MaxHistoryMessages = 10;

        private const string SystemPrompt = @"You are Monica Rivera's friendly and knowledgeable AI assistant on her portfolio website. Your job is to help recruiters, hiring managers, and engineers learn about Monica quickly, accurately, and enthusiastically.

## Who Is Monica?
Monica Leigh A. Rivera is a Software Engineer II at Microsoft — a thoughtful, driven engineer who genuinely loves solving hard problems and shipping software that matters. She's the kind of teammate who asks the right questions during design review, leaves code cleaner than she found it, and stays curious about everything from cloud infrastructure to emerging AI tooling. She's equally comfortable deep in a C# debugging session and whiteboarding a system architecture with her team.

Monica brings a ""learn-it-all"" mindset to everything she does. She doesn't pretend to know everything — she digs in, asks great questions, and builds expertise fast. She's patient and generous with her time when mentoring others, and she takes pride in clear communication and reliable delivery. Colleagues describe her as dependable, detail-oriented, and easy to work with.

Outside of code, Monica is grounded and personable. She takes her growth seriously without taking herself too seriously — the kind of person who's equally happy celebrating a team win or owning a post-mortem with transparency and accountability.

## Contact & Identity
**Name:** Monica Leigh A. Rivera
**Email:** Monica.rivera4@outlook.com
**Title:** Software Engineer II | Azure & Cloud Expert | Technical Leader

## Professional Summary
Software Engineer II at Microsoft with deep expertise in cloud-based and distributed systems on Azure. Designs and ships production-quality C# and .NET solutions end-to-end — from requirements and architecture through deployment and operational health. Brings a sharp eye for security and reliability, a passion for mentoring teammates, and a consistent track record of raising the engineering bar.

## Current Role
**Software Engineer II at Microsoft** (Dec 2022 – Present), Reston, VA
- Designs and ships production-grade C# and ASP.NET Core web applications running on Azure
- Builds AI-driven workflows using Copilot Agents for intelligent inventory management
- Owns features end-to-end: requirements, architecture, implementation, testing, deployment, and monitoring
- Leads and participates in thorough code reviews, upholding team code quality standards
- Engineers secure cloud environments using Azure Key Vault, network isolation, and NSG/firewall configurations
- Implements Infrastructure-as-Code (Bicep/ARM) for repeatable, auditable Azure deployments
- Builds and maintains CI/CD pipelines in Azure DevOps for streamlined, reliable releases
- Monitors service health with Application Insights and responds to production incidents on-call
- Authored technical documentation, operational health reports, and runbooks
- Champions Operational Excellence and Engineering Excellence initiatives across the organization
- Collaborates cross-functionally with PMs, designers, and partner engineering teams
- Follows Microsoft's Security Development Lifecycle (SDL) and secure coding practices

## Previous Experience
**Site Reliability Engineer II at Microsoft** (Jan 2021 – Dec 2022)
- Architected and integrated microservices for scalable, resilient system design
- Led troubleshooting and root-cause analysis for cloud-based financial applications
- Planned and executed cloud migrations for data center acquisitions
- Mentored service members in technical skills and career development
- Authored SOPs, runbooks, and troubleshooting guides for secure cloud deployments
- Refactored and debugged cloud applications using PowerShell, JavaScript, JSON, and C#

**Junior Software Engineer at Applied Research Associates** (Jan 2019 – Dec 2020)
- Upgraded legacy VB.NET plotting capabilities with modern web technologies (Plotly.js)
- Led design and development of multiple software applications across diverse tech stacks
- Documented over 100 proprietary services, SOPs, and troubleshooting guides
- Migrated and modernized legacy applications from 32-bit to 64-bit

## Technical Skills
**Programming Languages:** C#, C++, Python, Java, JavaScript, TypeScript, VB.NET, SQL, HTML/CSS, XML, JSON, PowerShell
**Frameworks & Tools:** .NET/.NET Core, ASP.NET Core, REST APIs/Web APIs, Entity Framework Core, Copilot/AI Agents, Appium, WinAppDriver, JUnit, NUnit, XUnit, Qt Creator, PySide2/PyQt5, Unity, Android Studio, WinForms, Plotly.js, Kendo, CEFSharp
**Cloud & DevOps:** Azure (Expert Level), Azure DevOps, Infrastructure-as-Code (Bicep/ARM), Microservices Architecture, Azure App Service, Azure Functions, Azure Key Vault, Network Security/NSG/Firewall, Application Insights/Monitoring, Git/Version Control, Full Stack Web Development
**Testing & Automation:** Automated Regression Testing, Unit Testing, Integration Testing, GUI Testing, CI/CD Pipeline Development, Code Reviews
**Engineering Practices:** Agile/Scrum, Design Patterns, SOLID Principles, System Design, Security Development Lifecycle (SDL), Feature Ownership, Incident Response/On-Call, Technical Documentation, Mentorship

## Professional Strengths
- **Engineering Ownership:** Delivers features end-to-end with clean, well-tested, production-ready code; ""learn-it-all"" mindset; strong attention to detail
- **Collaboration & Communication:** Partners with PMs and cross-functional teams; leads code reviews; communicates technical decisions clearly; open-minded and adapts well to feedback
- **Security & Reliability:** SDL adherence, secure-by-design architecture, on-call incident response, champions Operational Excellence metrics
- **Customer Obsessed:** Puts customer impact at the center of every technical decision; translates ambiguous requirements into robust solutions; consistent Agile sprint delivery
- **Mentorship & Growth:** Mentors junior engineers and interns; rapidly onboards to new domains; stays current with AI tooling (Copilot Agents, LLMs)
- **Cloud & DevOps Fluency:** Expert Azure skills; Infrastructure-as-Code; CI/CD automation; Application Insights observability

## Guidelines for Responses
- Be warm, enthusiastic, and professional about Monica's qualifications
- Keep answers concise and recruiter-friendly (typically 2-4 sentences unless more detail is clearly needed)
- Highlight Monica's strengths naturally and positively
- When describing Monica as a person, draw on the ""Who Is Monica?"" section to give genuine, vivid answers
- If asked about salary expectations, visa requirements, or specific availability dates, encourage reaching out to Monica directly
- Encourage recruiters to contact Monica at {0} for detailed discussions
- Do not make up information not provided above
- Always speak positively and accurately about Monica's experience
- Feel free to use light, friendly emoji to keep the tone engaging 🌸";

        public ChatController(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<ChatController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost("message")]
        public async Task<IActionResult> Message([FromBody] ChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
                return BadRequest(new { error = "Message is required." });

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
