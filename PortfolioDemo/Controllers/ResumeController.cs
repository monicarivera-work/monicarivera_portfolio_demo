using Microsoft.AspNetCore.Mvc;
using PortfolioDemo.Services.Resume;

namespace PortfolioDemo.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ResumeController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ResumeController> _logger;

        public ResumeController(IConfiguration configuration, ILogger<ResumeController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet("pdf")]
        public IActionResult DownloadPdf()
        {
            _logger.LogInformation("Resume PDF download requested");

            var data = BuildResumeData();
            var bytes = ResumePdfGenerator.Generate(data);

            return File(bytes, "application/pdf", "Monica_Rivera_Resume.pdf");
        }

        [HttpGet("docx")]
        public IActionResult DownloadDocx()
        {
            _logger.LogInformation("Resume DOCX download requested");

            var data = BuildResumeData();
            var bytes = ResumeDocxGenerator.Generate(data);

            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "Monica_Rivera_Resume.docx");
        }

        private ResumeData BuildResumeData()
        {
            var email = _configuration[Constants.EmailAddressKey];
            var phone = _configuration[Constants.PhoneNumberKey];
            return ResumeData.Build(email, phone);
        }
    }
}
