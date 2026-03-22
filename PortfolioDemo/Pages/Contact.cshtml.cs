using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net;
using System.Net.Mail;

namespace PortfolioDemo.Pages
{
    public class ContactModel : PageModel
    {
        private readonly ILogger<ContactModel> _logger;
        private readonly IConfiguration _configuration;

        public string? ContactEmail { get; private set; }
        public bool? MessageSent { get; private set; }

        public ContactModel(ILogger<ContactModel> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public void OnGet()
        {
            ContactEmail = _configuration[Constants.EmailAddressKey];
        }

        public async Task<IActionResult> OnPostAsync(
            string name, string email, string subject, string message)
        {
            ContactEmail = _configuration[Constants.EmailAddressKey];

            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(name)
                || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(subject)
                || string.IsNullOrWhiteSpace(message))
            {
                MessageSent = false;
                return Page();
            }

            // Basic email format validation
            if (!System.Text.RegularExpressions.Regex.IsMatch(email,
                @"^[^@\s]+@[^@\s]+\.[^@\s]+$", System.Text.RegularExpressions.RegexOptions.None,
                TimeSpan.FromMilliseconds(250)))
            {
                MessageSent = false;
                return Page();
            }

            var toAddress = _configuration[Constants.EmailAddressKey];
            if (string.IsNullOrWhiteSpace(toAddress))
            {
                _logger.LogWarning("EMAIL_ADDRESS is not configured. Cannot send contact form message.");
                MessageSent = false;
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
                    _logger.LogWarning("SMTP settings are not fully configured.");
                    MessageSent = false;
                    return Page();
                }

                // Sanitize inputs to prevent header injection: strip newlines from single-line fields
                var safeName = name.Replace("\r", "").Replace("\n", " ").Trim();
                var safeEmail = email.Replace("\r", "").Replace("\n", "").Trim();
                var safeSubject = subject.Replace("\r", "").Replace("\n", " ").Trim();

                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromAddress),
                    Subject = $"Portfolio Contact: {safeSubject}",
                    Body = $"Name: {safeName}\nEmail: {safeEmail}\n\n{message}",
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

                await smtpClient.SendMailAsync(mailMessage);
                _logger.LogInformation("Contact form message sent to {ToAddress}", toAddress);
                MessageSent = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send contact form email.");
                MessageSent = false;
            }

            return Page();
        }
    }
}
