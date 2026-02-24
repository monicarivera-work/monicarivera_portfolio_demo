using Microsoft.AspNetCore.Mvc;
using PortfolioDemo.Pages.Shared.Helpers;

namespace PortfolioDemo.Controllers
{
    public class FileController : Controller
    {
        private readonly FileShareHelper _fileHelper;

        public FileController(IConfiguration configuration)
        {
            var connectionString = configuration[Constants.AzureFileConnectionStringKey];
            _fileHelper = new FileShareHelper(connectionString);
        }

        public async Task<IActionResult> Download(string fileName)
        {
            var content = await _fileHelper.DownloadFileAsync(Constants.AzureFileShareName, fileName);
            return File(content, "application/octet-stream", fileName);
        }
    }
}
