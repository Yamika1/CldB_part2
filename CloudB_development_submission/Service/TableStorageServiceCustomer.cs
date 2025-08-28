using Azure;
using Azure.Data.Tables;
using CloudB_development_submission.Models;

namespace CloudB_development_submission.Service
{
    public class TableStorageServiceCustomer
    {
        public readonly TableClient _CustomertableClient;

        public TableStorageServiceCustomer(string connectionString)
        {

            _CustomertableClient = new TableClient(connectionString, "Customer");


        }

        public async Task<List<Customer>> GetAllCustomersAsync()
        {
            var customers = new List<Customer>();

            await foreach (var customer in _CustomertableClient.QueryAsync<Customer>())
            {
                customers.Add(customer);
            }

            return customers;
        }
        public async Task AddCustomerAsync(Customer customer)
        {
            if (string.IsNullOrEmpty(customer.PartitionKey) || string.IsNullOrEmpty(customer.RowKey))
            {
                throw new ArgumentException("PartitionKey and rowkey must be set");
            }
            try
            {
                await _CustomertableClient.AddEntityAsync(customer);

            }
            catch (RequestFailedException ex)
            {
                throw new InvalidOperationException("Error adding entity to Table Storage", ex);
            }
        }
        public async Task<Customer?> GetCustomerAsync(string partitionKey, string rowKey)
        {
            try
            {
                var response = await _CustomertableClient.GetEntityAsync<Customer>(partitionKey, rowKey);
                return response.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }
        public async Task<Customer?> GetCustomerByIdAsync(string partitionKey, string rowKey)
        {
            return await GetCustomerAsync(partitionKey, rowKey);
        }
        public async Task UpdateCustomerAsync(Customer customer)
        {
            if (string.IsNullOrEmpty(customer.PartitionKey) || string.IsNullOrEmpty(customer.RowKey))
            {
                throw new ArgumentException("PartitionKey and RowKey must be set");
            }

            await _CustomertableClient.UpdateEntityAsync(customer, ETag.All, TableUpdateMode.Replace);
        }



        public async Task DeleteCustomerAsync(string partitionKey, string rowKey)
        {
            await _CustomertableClient.DeleteEntityAsync(partitionKey, rowKey);
        }
        public async Task<List<Customer>> GetCustomerMessagesAsync()
        {
            var customers = new List<Customer>();

            await foreach (var customer in _CustomertableClient.QueryAsync<Customer>())
            {
                customers.Add(customer);
            }

            return customers;
        }

    }
}