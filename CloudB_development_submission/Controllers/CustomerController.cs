using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using CloudB_development_submission.Models;
using CloudB_development_submission.Service;
using Microsoft.AspNetCore.Mvc;

namespace CloudB_development_submission.Controllers
{
    public class CustomerController : Controller
    {
        private readonly TableStorageServiceCustomer _tableStorageService;
        private readonly BlobService _blobService;
        private readonly QueueService _svc;
        private readonly AzureFileShareService _fileShareService;

        private readonly string connectionstring = "DefaultEndpointsProtocol=https;AccountName=yamikagovenderstorage;AccountKey=YOUR-KEY;EndpointSuffix=core.windows.net";
        private readonly string _containerName = "productitem";

        public CustomerController(BlobService blobService, TableStorageServiceCustomer tableStorageService, AzureFileShareService fileShareService, QueueService svc)
        {
            _blobService = blobService;
            _tableStorageService = tableStorageService;
            _fileShareService = fileShareService;
            _svc = svc;
        }


        public async Task<IActionResult> Index()
        {
            var customers = await _tableStorageService.GetAllCustomersAsync();

            try
            {
                var localFiles = await _fileShareService.ListFilesAsync("yamikfileshare");
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
            return View(customers);
        }

        // GET: Add
        [HttpGet]
        public IActionResult AddCustomer() => View();

        // POST: Add
        [HttpPost]
        public async Task<IActionResult> AddCustomer(Customer customer, IFormFile? file)
        {
            if (!ModelState.IsValid)
            {
                TempData["message"] = "Invalid details";
                return RedirectToAction("Index");
            }

            customer.PartitionKey = "CustomerPartition";
            customer.RowKey = Guid.NewGuid().ToString();

            if (file != null && file.Length > 0)
            {
                using var stream = file.OpenReadStream();
                customer.ImageUrl = await _blobService.UploadAsync(stream, file.FileName);
            }

            await _tableStorageService.AddCustomerAsync(customer);
            TempData["message"] = "Customer added successfully!";
            return RedirectToAction("Index");
        }

        // GET: Edit
        [HttpGet]
        public async Task<IActionResult> Edit(string partitionKey, string rowKey)
        {
            var customer = await _tableStorageService.GetCustomerByIdAsync(partitionKey, rowKey);
            if (customer == null) return NotFound();
            return View(customer);
        }

        // POST: Edit
        [HttpPost]
        public async Task<IActionResult> Edit(Customer customer, IFormFile? file)
        {
            if (!ModelState.IsValid)
            {
                return View(customer);
            }

            if (file != null && file.Length > 0)
            {
                using var stream = file.OpenReadStream();
                customer.ImageUrl = await _blobService.UploadAsync(stream, file.FileName);
            }

            await _tableStorageService.UpdateCustomerAsync(customer);
            TempData["message"] = "Customer updated successfully!";
            return RedirectToAction("Index");
        }

        // POST: Delete
        [HttpPost]
        public async Task<IActionResult> Delete(string partitionKey, string rowKey, string? imageUrl)
        {
            if (!string.IsNullOrEmpty(imageUrl))
            {
                await _blobService.DeleteBlobAsync(imageUrl);
            }

            await _tableStorageService.DeleteCustomerAsync(partitionKey, rowKey);
            TempData["message"] = "Customer deleted successfully!";
            return RedirectToAction("Index");
        }
        [HttpPost]

        public async Task<IActionResult> AddCustomerImage(Customer customer, IFormFile file)
        {
            if (file == null)
            {

                using var stream = file.OpenReadStream();
                var imageUrl = await _blobService.UploadAsync(stream, file.FileName);
                customer.ImageUrl = imageUrl;

            }
            if (ModelState.IsValid)
            {
                customer.PartitionKey = "ItemPartition";
                customer.RowKey = Guid.NewGuid().ToString();
                await _tableStorageService.AddCustomerAsync(customer);
                return RedirectToAction("Index");
            }
            return View(customer);
        }
        [HttpPost]

        public async Task<IActionResult> DeleteCustomer(string partitionKey, string rowKey, Customer customer)
        {
            if (customer != null && !string.IsNullOrEmpty(customer.ImageUrl))
            {
                await _blobService.DeleteBlobAsync(customer.ImageUrl);

            }
            await _tableStorageService.DeleteCustomerAsync(partitionKey, rowKey);

            return RedirectToAction("Index");
        }
        // GET: Delete
        [HttpGet]
        public async Task<IActionResult> Delete(string partitionKey, string rowKey)
        {
            var customer = await _tableStorageService.GetCustomerByIdAsync(partitionKey, rowKey);
            if (customer == null) return NotFound();

            return View(customer);
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
    }
}