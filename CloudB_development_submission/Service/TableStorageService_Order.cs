using Azure;
using Azure.Data.Tables;
using CloudB_development_submission.Models;

namespace CloudB_development_submission.Service
{
    public class TableStorageService_Order
    {
        public readonly TableClient _OrdertableClient;

        public TableStorageService_Order(string connectionString)
        {

            _OrdertableClient = new TableClient(connectionString, "Order");


        }

        public async Task<List<Order>> GetAllOrdersAsync()
        {
            var orders = new List<Order>();

            await foreach (var order in _OrdertableClient.QueryAsync<Order>())
            {
                orders.Add(order);
            }

            return orders;
        }
        public async Task AddOrdersAsync(Order order)
        {
            if (string.IsNullOrEmpty(order.PartitionKey) || string.IsNullOrEmpty(order.RowKey))
            {
                throw new ArgumentException("PartitionKey and rowkey must be set");
            }
            try
            {
                await _OrdertableClient.AddEntityAsync(order);

            }
            catch (RequestFailedException ex)
            {
                throw new InvalidOperationException("Error adding entity to Table Storage", ex);
            }
        }
        public async Task<Order?> GetOrderAsync(string partitionKey, string rowKey)
        {
            try
            {
                var response = await _OrdertableClient.GetEntityAsync<Order>(partitionKey, rowKey);
                return response.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }
        public async Task<Order?> GetOrderByIdAsync(string rowKey)
        {
            await foreach (var order in _OrdertableClient.QueryAsync<Order>(o => o.RowKey == rowKey))
            {
                return order;
            }
            return null;
        }



        public async Task UpdateOrderAsync(Order order)
        {
            if (string.IsNullOrEmpty(order.PartitionKey) || string.IsNullOrEmpty(order.RowKey))
            {
                throw new ArgumentException("PartitionKey and RowKey must be set");
            }

            await _OrdertableClient.UpdateEntityAsync(order, ETag.All, TableUpdateMode.Replace);
        }
        public async Task DeleteOrderAsync(string partitionKey, string rowKey)
        {
            await _OrdertableClient.DeleteEntityAsync(partitionKey, rowKey);
        }
        public async Task<List<Order>> GetOrderMessagesAsync()
        {
            var orders = new List<Order>();

            await foreach (var order in _OrdertableClient.QueryAsync<Order>())
            {
                orders.Add(order);
            }

            return orders;
        }

    }
}