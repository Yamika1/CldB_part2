using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using CloudB_development_submission.Models;
using CloudB_development_submission.Service;
using Microsoft.AspNetCore.Mvc;

namespace CloudB_development_submission.Controllers
{
    public class OrderController : Controller
    {
        private readonly TableStorageService_Order _tableStorageService;
        private readonly BlobService _blobService;
        private readonly QueueService _svc;
        private readonly AzureFileShareService _fileShareService;
        private readonly string connectionstring = "DefaultEndpointsProtocol=https;AccountName=yamikagovenderstorage;AccountKey=GqYG0wpA/tgxt2Sm8vT4VVwfdBxYid7k/pQfq926k618XQU1OsEm2S6aR16i7qWxGw6ih6/wxBed+AStHyHZgw==;EndpointSuffix=core.windows.net";
        private readonly string _containerName = "productitem";

        public OrderController(BlobService blobService, TableStorageService_Order tableStorageService, AzureFileShareService fileShareService, QueueService svc)
        {
            _blobService = blobService;
            _tableStorageService = tableStorageService;
            _fileShareService = fileShareService;
            _svc = svc;
        }
        public async Task<IActionResult> Index()
        {

            var orders = await _tableStorageService.GetAllOrdersAsync();


            try
            {
                var localFiles = await _fileShareService.ListFilesAsync("uploads");
                ViewBag.LocalFiles = localFiles;
            }
            catch
            {
                ViewBag.LocalFiles = new List<FileModel>();
            }


            try
            {
                var blobFiles = await _blobService.GetAllBlobsAsync();
                ViewBag.BlobFiles = blobFiles;
            }
            catch
            {
                ViewBag.BlobFiles = new List<string>();
            }

            try
            {
                var queueMessages = await _svc.PeekMessagesAsync(5);
                ViewBag.QueueMessages = queueMessages;
            }
            catch
            {
                ViewBag.QueueMessages = new List<string>();
            }

            return View(orders);
        }

        public async Task<IActionResult> Details(string partitionKey, string rowKey)
        {
            if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
                return NotFound();

            var order = await _tableStorageService.GetOrderAsync(partitionKey, rowKey); // Your method may vary

            if (order == null)
                return NotFound();

            return View(order);
        }



        public async Task<IActionResult> Delete(string partitionKey, string rowKey)
        {
            await _tableStorageService.DeleteOrderAsync(partitionKey, rowKey);
            return RedirectToAction("Index");
        }
        [HttpPost]

        public async Task<IActionResult> AddOrder(Order order)
        {
            order.PartitionKey = "OrderPartition";
            order.RowKey = Guid.NewGuid().ToString();

            await _tableStorageService.AddOrdersAsync(order);
            return RedirectToAction("Index");
        }
        [HttpPost]

        public async Task<IActionResult> AddOrderImage(Order order, IFormFile file)
        {
            if (file != null)
            {
                using var stream = file.OpenReadStream();
                var imageUrl = await _blobService.UploadAsync(stream, file.FileName);
                order.ImageUrl = imageUrl;
            }

            if (ModelState.IsValid)
            {
                order.PartitionKey = "ItemPartition";
                order.RowKey = Guid.NewGuid().ToString();
                await _tableStorageService.AddOrdersAsync(order);
                return RedirectToAction("Index");
            }
            return View(order);
        }

        [HttpPost]

        public async Task<IActionResult> DeleteOrder(string partitionKey, string rowKey, Order order)
        {
            if (order != null && !string.IsNullOrEmpty(order.ImageUrl))
            {
                await _blobService.DeleteBlobAsync(order.ImageUrl);

            }
            await _tableStorageService.DeleteOrderAsync(partitionKey, rowKey);

            return RedirectToAction("Index");

        }
        [HttpPost]
        public async Task<IActionResult> UpdatePaymentOption(string partitionKey, string rowKey, string paymentOption)
        {
            var order = await _tableStorageService.GetOrderAsync(partitionKey, rowKey);
            if (order == null)
                return NotFound();

            order.PaymentOption = paymentOption;
            await _tableStorageService.UpdateOrderAsync(order);

            TempData["message"] = "Payment option updated successfully!";
            return RedirectToAction("Details", new { partitionKey = order.PartitionKey, rowKey = order.RowKey });
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
        [HttpGet]
        public IActionResult Upload() => View();


        // POST: Upload to File Share
        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile? file)
        {
            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError("file", "Please select a file to upload");
                return await Index();
            }

            try
            {
                using (var stream = file.OpenReadStream())
                {
                    string directoryName = "uploads";
                    string fileName = file.FileName;
                    await _fileShareService.UploadFileAsync(directoryName, fileName, stream);
                }
                TempData["message"] = $"File '{file.FileName}' uploaded successfully";
            }
            catch (Exception e)
            {
                TempData["message"] = $"File upload failed: {e.Message}";
            }

            return RedirectToAction("Index");
        }

        // GET: Download from File Share
        [HttpGet]
        public async Task<IActionResult> DownloadFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return BadRequest("File name cannot be null or empty");
            }

            try
            {
                var fileStream = await _fileShareService.DownloadFileAsync("uploads", fileName);
                if (fileStream == null)
                {
                    return NotFound($"File '{fileName}' not found");
                }

                return File(fileStream, "application/octet-stream", fileName);
            }
            catch (Exception e)
            {
                return BadRequest($"Error downloading file: {e.Message}");
            }
        }
        [HttpGet] public IActionResult Send() => View();
        [HttpPost]

        public async Task<IActionResult> Send(string? message)
        {
            if (string.IsNullOrWhiteSpace(message)) { ViewBag.Msg = "Enter a message"; return View(); }
            await _svc.SendAsync(message);
            ViewBag.Msg = "message enqueued";
            return View();

        }

        [HttpGet]
        public IActionResult AddOrder()
        {
            return View();
        }
    }

}
