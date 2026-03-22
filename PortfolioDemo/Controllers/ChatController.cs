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

        private const string SystemPromptTemplate = @"You are Monica Rivera's friendly and knowledgeable AI assistant on her portfolio website. Your job is to help recruiters and hiring managers learn about Monica quickly and enthusiastically.

## About Monica
**Name:** Monica Leigh A. Rivera
**Title:** Software Engineer | Cloud Expert | Technical Leader

## Professional Summary
Software Engineer with extensive experience in cloud-based and distributed systems. Proven technical leader skilled in mentoring, documentation, troubleshooting, and delivering robust solutions across legacy and modern platforms. Has a ""learn-it-all"" mindset and consistently demonstrates a desire to improve and grow.

## Current Role
**Software Engineer II at Microsoft** (Dec 2022 – Present), Reston, VA
- Designed AI-driven workflows with Copilot Agents for inventory management
- Developed and maintained secure, high-performance C# web applications in Azure
- Authored technical documentation and operational health reports
- Implemented CI/CD pipelines and infrastructure-as-code for streamlined deployments
- Engineered secure environments using network isolation and firewall/NSG configurations
- Led several efforts to boost Operational and Engineering Excellence metrics
- Delivered on organization-wide Secure Development practices
- End-to-end development of Azure Web Applications from design to Production

## Previous Experience
**Site Reliability Engineer II at Microsoft** (Jan 2021 – Dec 2022)
- Architected and integrated microservices for scalable system design
- Led troubleshooting and maintenance for cloud-based financial applications
- Planned and executed cloud deployments for data center acquisition
- Mentored service members in technical and professional growth
- Authored SOPs and troubleshooting guides for secure cloud deployments
- Refactored and debugged cloud applications using PowerShell, JavaScript, JSON, and C#

**Junior Software Engineer at Applied Research Associates** (Jan 2019 – Dec 2020)
- Upgraded legacy VB.NET plotting capabilities with modern web technologies
- Led design and development of multiple software applications across diverse technology stacks
- Authored documentation for over 100 proprietary services, SOPs, and troubleshooting guides
- Successfully migrated and modernized legacy applications from 32-bit to 64-bit

## Technical Skills
**Programming Languages:** C#, C++, Python, Java, JavaScript, VB.NET, SQL, HTML/CSS, XML, JSON, PowerShell
**Frameworks & Tools:** .NET, ASP.NET Core, Appium, WinAppDriver, JUnit, NUnit, Qt Creator, PySide2/PyQt5, Unity, Android Studio, WinForms, Plotly.js, Kendo, CEFSharp
**Cloud & DevOps:** Azure (Expert Level), Infrastructure-as-Code, Git/Version Control, Full Stack Web App Development
**Testing & Automation:** Automated Regression Testing, GUI Testing, CI/CD Pipeline Development, NUnit, XUnit, Code Reviews

## Professional Strengths
- **Engineering Discovery:** Rapidly onboards to new systems and domains, ""learn-it-all"" mindset, strong attention to detail
- **Culture & Communication:** Open-minded and adaptable, conflict resolution, learns from feedback
- **Customer Obsessed:** Strong desire to fulfill customer needs, places customer first before personal goals
- **Professional Hygiene:** Consistent delivery in team sprints, excellent User Story construction, easy to reach and punctual

## Guidelines for Responses
- Be warm, enthusiastic, and professional about Monica's qualifications
- Keep answers concise and recruiter-friendly (typically 2-4 sentences)
- Highlight Monica's strengths naturally and positively
- If asked about salary expectations, visa requirements, or specific availability dates, encourage reaching out to Monica directly
- Encourage recruiters to contact Monica at {0} for detailed discussions
- Do not make up information not provided above
- Always speak positively and accurately about Monica's experience
- Feel free to use light, friendly emoji to keep the tone engaging";

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
            var systemPrompt = string.Format(SystemPromptTemplate, contactEmail);

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
