using Azure.Storage.Queues;
using CloudB_development_submission.Service;
using Microsoft.Extensions.Configuration;

namespace CloudB_development_submission
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var configuration = builder.Configuration;

            // Add services to the container.
            builder.Services.AddControllersWithViews();
            builder.Services.AddSingleton(new TableStorageService(configuration.GetConnectionString("AzureStorage"))
                   );
            builder.Services.AddSingleton<TableStorageServiceCustomer>(sp =>
    new TableStorageServiceCustomer(
        builder.Configuration.GetConnectionString("AzureStorage")));


            builder.Services.AddSingleton<TableStorageService_Order>(sp =>
                new TableStorageService_Order(
                    builder.Configuration.GetConnectionString("AzureStorage")));
            builder.Services.AddSingleton(new BlobService(configuration.GetConnectionString("AzureStorage"))
          );
            builder.Services.AddSingleton<QueueService>(sp =>
            {
                var connectionString = configuration.GetConnectionString("AzureStorage");
                var queueName = "yamikagov";

                var queueClient = new QueueClient(connectionString, queueName);


                queueClient.CreateIfNotExists();

                return new QueueService(queueClient);
            });
            builder.Services.AddSingleton<AzureFileShareService>(sp =>
            {
                var connectionstring = configuration.GetConnectionString("AzureStorage");
                return new AzureFileShareService(connectionstring, "yamikfileshare");
            });
            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
