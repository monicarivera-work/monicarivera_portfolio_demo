using Microsoft.AspNetCore.Mvc;
using PortfolioDemo.Pages.Shared.Helpers;

namespace PortfolioDemo.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FileController : Controller
    {
        private readonly FileShareHelper _fileHelper;
        private readonly ILogger<FileController> _logger;

        public FileController(IConfiguration configuration, ILogger<FileController> logger)
        {
            _logger = logger;
            var connectionString = configuration[Constants.AzureFileConnectionStringKey];
            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogError("Azure File Connection String is not configured.");
            }
            _fileHelper = new FileShareHelper(connectionString);
        }

        [HttpGet("download")]
        public async Task<IActionResult> Download(string fileName)
        {
            _logger.LogInformation($"Download requested for file: {fileName}");

            try
            {
                var content = await _fileHelper.DownloadFileAsync(Constants.AzureFileShareName, fileName);
                _logger.LogInformation($"File {fileName} downloaded successfully. Size: {content.Length} bytes");
                return File(content, "application/octet-stream", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error downloading file {fileName}: {ex.Message}");
                return NotFound($"File not found: {fileName}");
            }
        }
    }
}
