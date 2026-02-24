using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;

namespace PortfolioDemo.Pages.Shared.Helpers
{
    public class FileShareHelper
    {
        private readonly string _connectionString;
        
        public FileShareHelper(string connectionString)
        {
            _connectionString = connectionString;
        }

        //Download file from Azure File Share
        public async Task<byte[]> DownloadFileAsync(string shareName, string filePath)
        {
            //Create client for the share
            ShareClient shareClient = new ShareClient(_connectionString, shareName);
            ShareDirectoryClient directoryClient = shareClient.GetRootDirectoryClient();

            //Get file client
            ShareFileClient fileClient = directoryClient.GetFileClient(filePath);

            if (!await fileClient.ExistsAsync())
            {
                throw new FileNotFoundException($"File '{filePath}' not found in share '{shareName}'.");
            }

            //Download content into memory
            ShareFileDownloadInfo download = await fileClient.DownloadAsync();
            using (MemoryStream ms = new MemoryStream())
            {
                await download.Content.CopyToAsync(ms);
                return ms.ToArray();
            }
        }
    }
}
