using Azure;
using Azure.Data.Tables;
using CloudB_development_submission.Models;

namespace CloudB_development_submission.Service
{
    public class TableStorageService
    {
        public readonly TableClient _ProducttableClient;



        public TableStorageService(string connectionString)
        {

            _ProducttableClient = new TableClient(connectionString, "Product");


        }

        public async Task<List<Product>> GetAllProductsAsync()
        {
            var products = new List<Product>();

            await foreach (var product in _ProducttableClient.QueryAsync<Product>())
            {
                products.Add(product);

            }
            return products;
        }
        public async Task AddProductAsync(Product product)
        {
            if (string.IsNullOrEmpty(product.PartitionKey) || string.IsNullOrEmpty(product.RowKey))
            {
                throw new ArgumentException("PartitionKey and rowkey must be set");
            }
            try
            {
                await _ProducttableClient.AddEntityAsync(product);

            }
            catch (RequestFailedException ex)
            {
                throw new InvalidOperationException("Error adding entity to Table Storage", ex);
            }
        }
        public async Task<Product?> GetProductByIdAsync(string partitionKey, string rowKey)
        {
            try
            {
                var response = await _ProducttableClient.GetEntityAsync<Product>(partitionKey, rowKey);
                return response.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null; 
            }
        }

        public async Task DeletProductAsync(string partitionKey, string rowKey)
        {
            await _ProducttableClient.DeleteEntityAsync(partitionKey, rowKey);
        }

       
        public async Task<List<Product>> GetAllItemsAsync()
        {
            var products = new List<Product>();
            await foreach (var product in _ProducttableClient.QueryAsync<Product>())
            {
                products.Add(product);
            }
            return products;
        }

        public async Task UpdateProductAsync(Product product)
        {
            if (string.IsNullOrEmpty(product.PartitionKey) || string.IsNullOrEmpty(product.RowKey))
            {
                throw new ArgumentException("PartitionKey and RowKey must be set");
            }

            await _ProducttableClient.UpdateEntityAsync(product, ETag.All, TableUpdateMode.Replace);
        }
    }
}