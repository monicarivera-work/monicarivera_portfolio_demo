using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;

namespace PortfolioDemo.Services
{
    public class FileShareHelper
    {
        private readonly string _connectionString;

        public FileShareHelper(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                throw new ArgumentException("Connection string must not be null or empty.", nameof(connectionString));
            _connectionString = connectionString;
        }

        public async Task<byte[]> DownloadFileAsync(string shareName, string filePath)
        {
            ShareClient shareClient = new ShareClient(_connectionString, shareName);
            ShareDirectoryClient directoryClient = shareClient.GetRootDirectoryClient();

            ShareFileClient fileClient = directoryClient.GetFileClient(filePath);

            if (!await fileClient.ExistsAsync())
            {
                throw new FileNotFoundException($"File '{filePath}' not found in share '{shareName}'.");
            }

            ShareFileDownloadInfo download = await fileClient.DownloadAsync();
            using var ms = new MemoryStream();
            await download.Content.CopyToAsync(ms);
            return ms.ToArray();
        }
    }
}
