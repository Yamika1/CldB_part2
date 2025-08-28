using Azure;
using Azure.Data.Tables;
using System.ComponentModel.DataAnnotations;

namespace CloudB_development_submission.Models
{
    public class Product : ITableEntity
    {
        [Key]

        public int ProductId { get; set; }

        public string? ProductName { get; set; }

        public string? ProductDescription { get; set; }

        public string? ProductType { get; set; }

        public double? ProductPrice { get; set; }

        public int? Quantity { get; set; }

        public string? First_Name { get; set; }

        public string? Last_Name { get; set; }

        public string? ImageURL { get; set; }

        // ITableEntity implementation
        public string? PartitionKey { get; set; }

        public string? RowKey { get; set; }

        public ETag ETag { get; set; }

        public DateTimeOffset? Timestamp { get; set; }



    }
}