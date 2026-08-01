using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PortfolioDemo.Services;

namespace PortfolioDemo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FileController : ControllerBase
    {
        private readonly FileShareHelper? _fileHelper;
        private readonly ILogger<FileController> _logger;

        public FileController(IConfiguration configuration, ILogger<FileController> logger)
        {
            _logger = logger;
            var connectionString = configuration[Constants.AzureFileConnectionStringKey];
            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogError("Azure File Connection String is not configured.");
            }
            else
            {
                _fileHelper = new FileShareHelper(connectionString);
            }
        }

        private static readonly string[] AllowedExtensions = { ".pdf", ".docx", ".doc", ".txt" };

        [HttpGet("download")]
        [EnableRateLimiting("FileDownloadRateLimit")]
        public async Task<IActionResult> Download(string fileName)
        {
            if (_fileHelper == null)
            {
                _logger.LogError("File download requested but Azure File Connection String is not configured.");
                return StatusCode(503, "File service is not available.");
            }

            if (string.IsNullOrWhiteSpace(fileName))
                return BadRequest("File name is required.");

            // Strip any path components to prevent path traversal attacks
            var safeFileName = Path.GetFileName(fileName);

            // Reject if stripping changed the name (path traversal attempt) or if name is empty
            if (string.IsNullOrWhiteSpace(safeFileName) || safeFileName != fileName)
            {
                _logger.LogWarning("Rejected download request with potentially unsafe file name: {FileName}", fileName);
                return BadRequest("Invalid file name.");
            }

            // Only allow known-safe file extensions
            var extension = Path.GetExtension(safeFileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
            {
                _logger.LogWarning("Rejected download request for disallowed file extension: {FileName}", safeFileName);
                return BadRequest("File type not allowed.");
            }

            _logger.LogInformation("Download requested for file: {FileName}", safeFileName);

            try
            {
                var content = await _fileHelper.DownloadFileAsync(Constants.AzureFileShareName, safeFileName);
                _logger.LogInformation("File {FileName} downloaded successfully. Size: {Size} bytes", safeFileName, content.Length);
                return File(content, "application/octet-stream", safeFileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading file {FileName}", safeFileName);
                return NotFound("File not found.");
            }
        }
    }
}
