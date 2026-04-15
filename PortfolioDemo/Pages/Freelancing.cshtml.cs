using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Mail;

namespace PortfolioDemo.Pages
{
    public class FreelancingModel : PageModel
    {
        private readonly ILogger<FreelancingModel> _logger;
        private readonly IConfiguration _configuration;

        [BindProperty]
        public FreelanceInquiryInput Inquiry { get; set; } = new();

        public bool? InquirySent { get; private set; }
        public string? InquiryErrorMessage { get; private set; }

        public FreelancingModel(ILogger<FreelancingModel> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public void OnGet()
        {
            _logger.LogInformation("Freelancing page visited");
        }

        public async Task<IActionResult> OnPostAsync()
        {
            _logger.LogInformation(
                "Freelancing inquiry received. SummaryLength={SummaryLength}",
                Inquiry.ProjectSummary?.Length ?? 0);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Freelancing inquiry rejected due to validation errors");
                InquirySent = false;
                InquiryErrorMessage = "Please fix the highlighted fields and try again.";
                return Page();
            }

            var toAddress = _configuration["EMAIL_ADDRESS"];
            if (string.IsNullOrWhiteSpace(toAddress))
            {
                _logger.LogWarning("EMAIL_ADDRESS is not configured. Cannot send freelancing inquiry.");
                InquirySent = false;
                InquiryErrorMessage = "Inquiry delivery is temporarily unavailable. Please try again shortly.";
                return Page();
            }

            try
            {
                var smtpHost = _configuration["SmtpSettings:Host"];
                var smtpPort = _configuration.GetValue<int>("SmtpSettings:Port", 587);
                var smtpUser = _configuration["SmtpSettings:Username"];
                var smtpPass = _configuration["SmtpSettings:Password"];
                var fromAddress = _configuration["SmtpSettings:FromAddress"];
                var enableSsl = _configuration.GetValue<bool>("SmtpSettings:EnableSsl", true);

                if (string.IsNullOrWhiteSpace(smtpHost) || string.IsNullOrWhiteSpace(fromAddress))
                {
                    _logger.LogWarning("SMTP settings are not fully configured for freelancing inquiry.");
                    InquirySent = false;
                    InquiryErrorMessage = "Inquiry delivery is temporarily unavailable. Please try again shortly.";
                    return Page();
                }

                var safeName = SanitizeSingleLine(Inquiry.Name);
                var safeEmail = SanitizeSingleLine(Inquiry.Email);
                var safeCompany = SanitizeSingleLine(Inquiry.Company);
                var safeDesiredSolution = SanitizeSingleLine(Inquiry.DesiredSolution);
                var safeTimeline = SanitizeSingleLine(Inquiry.Timeline);

                var body =
                    $"Name: {safeName}\n" +
                    $"Email: {safeEmail}\n" +
                    $"Company: {(string.IsNullOrWhiteSpace(safeCompany) ? "N/A" : safeCompany)}\n" +
                    $"Desired Solution: {safeDesiredSolution}\n" +
                    $"Timeline: {(string.IsNullOrWhiteSpace(safeTimeline) ? "N/A" : safeTimeline)}\n\n" +
                    $"Project Summary:\n{Inquiry.ProjectSummary?.Trim()}";

                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromAddress),
                    Subject = $"Freelancing Inquiry: {safeDesiredSolution}",
                    Body = body,
                    IsBodyHtml = false
                };
                mailMessage.To.Add(toAddress);

                using var smtpClient = new SmtpClient(smtpHost, smtpPort)
                {
                    EnableSsl = enableSsl,
                    Credentials = string.IsNullOrWhiteSpace(smtpUser)
                        ? null
                        : new NetworkCredential(smtpUser, smtpPass)
                };

                _logger.LogInformation("Attempting to send freelancing inquiry email");
                await smtpClient.SendMailAsync(mailMessage);
                _logger.LogInformation("Freelancing inquiry email sent successfully");
                ModelState.Clear();
                InquirySent = true;
                InquiryErrorMessage = null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send freelancing inquiry email.");
                InquirySent = false;
                InquiryErrorMessage = "I couldn't send your request right now. Please try again shortly.";
            }

            return Page();
        }

        private static string SanitizeSingleLine(string? value) =>
            value?.Replace("\r", string.Empty).Replace("\n", " ").Trim() ?? string.Empty;

    }

    public class FreelanceInquiryInput
    {
        [Required]
        [Display(Name = "Your Name")]
        [StringLength(80)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [Display(Name = "Work Email")]
        [StringLength(200)]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Business Name (Optional)")]
        [StringLength(120)]
        public string? Company { get; set; }

        [Required]
        [Display(Name = "What do you need built?")]
        [StringLength(120)]
        public string DesiredSolution { get; set; } = string.Empty;

        [Display(Name = "Preferred timeline (Optional)")]
        [StringLength(120)]
        public string? Timeline { get; set; }

        [Required]
        [Display(Name = "Brief summary of your app or web app")]
        [StringLength(3000, MinimumLength = 20)]
        public string ProjectSummary { get; set; } = string.Empty;
    }
}
