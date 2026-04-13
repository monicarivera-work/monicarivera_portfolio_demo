using System.Net;
using System.Net.Mail;
using System.Text.Json;

namespace PortfolioDemo.Services
{
    public record UnansweredQuestion
    {
        public string Question { get; init; } = string.Empty;
        public DateTime AskedAt { get; init; }
    }

    public interface IUnansweredQuestionService
    {
        Task RecordQuestionAsync(string question);
        Task<IReadOnlyList<UnansweredQuestion>> GetQuestionsAsync();
    }

    public class UnansweredQuestionService : IUnansweredQuestionService
    {
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;
        private readonly ILogger<UnansweredQuestionService> _logger;
        private readonly SemaphoreSlim _fileLock = new(1, 1);

        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        private string DataFilePath =>
            Path.Combine(_env.ContentRootPath, "Data", "unanswered_questions.json");

        public UnansweredQuestionService(
            IWebHostEnvironment env,
            IConfiguration configuration,
            ILogger<UnansweredQuestionService> logger)
        {
            _env = env;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task RecordQuestionAsync(string question)
        {
            await SaveToFileAsync(question);
            await TrySendEmailNotificationAsync(question);
        }

        public async Task<IReadOnlyList<UnansweredQuestion>> GetQuestionsAsync()
        {
            await _fileLock.WaitAsync();
            try
            {
                return await ReadQuestionsFromFileAsync();
            }
            finally
            {
                _fileLock.Release();
            }
        }

        private async Task SaveToFileAsync(string question)
        {
            await _fileLock.WaitAsync();
            try
            {
                var dataDir = Path.GetDirectoryName(DataFilePath)!;
                Directory.CreateDirectory(dataDir);

                var questions = await ReadQuestionsFromFileAsync();
                var updated = questions.ToList();
                updated.Add(new UnansweredQuestion { Question = question, AskedAt = DateTime.UtcNow });

                var json = JsonSerializer.Serialize(updated, JsonOptions);
                await File.WriteAllTextAsync(DataFilePath, json);

                _logger.LogInformation("Unanswered question recorded (length: {Length})", question.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save unanswered question to file");
            }
            finally
            {
                _fileLock.Release();
            }
        }

        private async Task<List<UnansweredQuestion>> ReadQuestionsFromFileAsync()
        {
            if (!File.Exists(DataFilePath))
                return new List<UnansweredQuestion>();

            try
            {
                var json = await File.ReadAllTextAsync(DataFilePath);
                return JsonSerializer.Deserialize<List<UnansweredQuestion>>(json)
                       ?? new List<UnansweredQuestion>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not read unanswered questions file; treating as empty");
                return new List<UnansweredQuestion>();
            }
        }

        private async Task TrySendEmailNotificationAsync(string question)
        {
            var toAddress = _configuration[Constants.EmailAddressKey];
            var smtpHost = _configuration["SmtpSettings:Host"];
            var fromAddress = _configuration["SmtpSettings:FromAddress"];

            if (string.IsNullOrWhiteSpace(toAddress)
                || string.IsNullOrWhiteSpace(smtpHost)
                || string.IsNullOrWhiteSpace(fromAddress))
            {
                _logger.LogInformation(
                    "SMTP not configured; skipping email notification for unanswered question");
                return;
            }

            try
            {
                var smtpPort = _configuration.GetValue<int>("SmtpSettings:Port", 587);
                var smtpUser = _configuration["SmtpSettings:Username"];
                var smtpPass = _configuration["SmtpSettings:Password"];
                var enableSsl = _configuration.GetValue<bool>("SmtpSettings:EnableSsl", true);

                using var mail = new MailMessage
                {
                    From = new MailAddress(fromAddress),
                    Subject = "Portfolio AI: Unanswered Question",
                    Body = $"Someone asked your AI assistant a question it couldn't answer:\n\n"
                           + $"\"{question}\"\n\nAsked at: {DateTime.UtcNow:u}",
                    IsBodyHtml = false
                };
                mail.To.Add(toAddress);

                using var smtp = new SmtpClient(smtpHost, smtpPort)
                {
                    EnableSsl = enableSsl,
                    Credentials = string.IsNullOrWhiteSpace(smtpUser)
                        ? null
                        : new NetworkCredential(smtpUser, smtpPass)
                };

                await smtp.SendMailAsync(mail);
                _logger.LogInformation("Email notification sent for unanswered question");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send email notification for unanswered question");
            }
        }
    }
}
