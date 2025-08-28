using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace CloudB_development_submission.Service
{
    public class BlobService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string connectionstring = "DefaultEndpointsProtocol=https;AccountName=yamikagovenderstorage;AccountKey=GqYG0wpA/tgxt2Sm8vT4VVwfdBxYid7k/pQfq926k618XQU1OsEm2S6aR16i7qWxGw6ih6/wxBed+AStHyHZgw==;EndpointSuffix=core.windows.net";
        private readonly string _containerName = "productitem";

        public BlobService(string connectionstring)
        {
            _blobServiceClient = new BlobServiceClient(connectionstring);
        }
        public async Task<string> UploadAsync(Stream filestream, string fileName)

        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(fileName);
            await blobClient.UploadAsync(filestream);
            return blobClient.Uri.ToString();

        }
        public async Task DeleteBlobAsync(string blobUri)
        {
            Uri uri = new Uri(blobUri);
            string blobName = uri.Segments[^1];
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(blobName);
            await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots);

        }
        public async Task<List<string>> GetAllBlobsAsync()
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobs = new List<string>();

            await foreach (var blob in containerClient.GetBlobsAsync())
            {
                blobs.Add(containerClient.GetBlobClient(blob.Name).Uri.ToString());
            }

            return blobs;
        }

        private async Task<List<string?>> FetchImageUrlsAsync()
        {
            var imageUrls = new List<string>();
            var containerClient = new BlobContainerClient(connectionstring, _containerName);

            await foreach (BlobItem blobItem in containerClient.GetBlobsAsync())
            {
                var blobClient = containerClient.GetBlobClient(blobItem.Name);
                imageUrls.Add(blobClient.Uri.ToString());

            }
            return imageUrls;
        }

        private async Task UploadFileToBlobStorageAsync(IFormFile uploadedFile)
        {
            var containerClient = new BlobContainerClient(connectionstring, _containerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

            var blobClient = containerClient.GetBlobClient(uploadedFile.FileName);

            using (var stream = uploadedFile.OpenReadStream())
            {
                await blobClient.UploadAsync(stream, true);
            }
        }
    }
}