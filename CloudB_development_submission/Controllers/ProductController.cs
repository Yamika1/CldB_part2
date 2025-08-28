using CloudB_development_submission.Models;
using CloudB_development_submission.Service;
using Microsoft.AspNetCore.Mvc;

namespace CloudB_development_submission.Controllers
{
    public class ProductController : Controller
    {
        private readonly TableStorageService _tableStorageService;
        private readonly BlobService _blobService;
        private readonly QueueService _svc;
        private readonly AzureFileShareService _fileShareService;
        private readonly string _connectionString = "DefaultEndpointsProtocol=https;AccountName=yamikagovenderstorage;AccountKey=...;EndpointSuffix=core.windows.net";
        private readonly string _containerName = "productitem";

        public ProductController(BlobService blobService, TableStorageService tableStorageService, AzureFileShareService fileShareService, QueueService svc)
        {
            _blobService = blobService;
            _tableStorageService = tableStorageService;
            _fileShareService = fileShareService;
            _svc = svc;
        }


        public async Task<IActionResult> Index()
        {
            var products = await _tableStorageService.GetAllProductsAsync();

            try
            {
                ViewBag.LocalFiles = await _fileShareService.ListFilesAsync("yamikfileshare");
            }
            catch { ViewBag.LocalFiles = new List<FileModel>(); }

            try
            {
                ViewBag.BlobFiles = await _blobService.GetAllBlobsAsync();
            }
            catch { ViewBag.BlobFiles = new List<string>(); }

            try
            {
                ViewBag.QueueMessages = await _svc.PeekMessagesAsync(5);
            }
            catch { ViewBag.QueueMessages = new List<string>(); }

            return View(products);
        }


        [HttpGet]
        public IActionResult AddProduct()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddProduct(Product product, IFormFile? file)
        {
            if (!ModelState.IsValid)
                return View(product);

            product.PartitionKey = "ProductPartition";
            product.RowKey = Guid.NewGuid().ToString();

            // Upload image if provided
            if (file != null)
            {
                using var stream = file.OpenReadStream();
                var imageUrl = await _blobService.UploadAsync(stream, file.FileName);
                product.ImageURL = imageUrl;
            }

            await _tableStorageService.AddProductAsync(product);
            return RedirectToAction("Index");
        }
        [HttpGet]
        public async Task<IActionResult> Edit(string partitionKey, string rowKey)
        {
            if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
                return NotFound();

            var product = await _tableStorageService.GetProductByIdAsync(partitionKey, rowKey);

            if (product == null)
                return NotFound();

            return View(product); // returns Edit.cshtml with product model
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Product product)
        {
            if (ModelState.IsValid)
            {
                await _tableStorageService.UpdateProductAsync(product);
                TempData["message"] = "Product updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(product);
        }


        [HttpPost]
        public async Task<IActionResult> Delete(string partitionKey, string rowKey, string? imageUrl)
        {
            if (!string.IsNullOrEmpty(imageUrl))
            {
                await _blobService.DeleteBlobAsync(imageUrl);
            }

            await _tableStorageService.DeletProductAsync(partitionKey, rowKey);
            return RedirectToAction("Index");
        }


        [HttpGet]
        public IActionResult Upload() => View();

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
                    await _fileShareService.UploadFileAsync("uploads", file.FileName, stream);
                }
                TempData["message"] = $"File '{file.FileName}' uploaded successfully";
            }
            catch (Exception e)
            {
                TempData["message"] = $"File upload failed: {e.Message}";
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> DownloadFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return BadRequest("File name cannot be null or empty");

            try
            {
                var fileStream = await _fileShareService.DownloadFileAsync("uploads", fileName);
                if (fileStream == null) return NotFound($"File '{fileName}' not found");

                return File(fileStream, "application/octet-stream", fileName);
            }
            catch (Exception e)
            {
                return BadRequest($"Error downloading file: {e.Message}");
            }
        }


        [HttpGet]
        public IActionResult Send() => View();

        [HttpPost]
        public async Task<IActionResult> Send(string? message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                ViewBag.Msg = "Enter a message";
                return View();
            }

            await _svc.SendAsync(message);
            ViewBag.Msg = "Message enqueued";
            return View();
        }
    }
}
