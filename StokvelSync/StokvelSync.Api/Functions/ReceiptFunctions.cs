using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using StokvelSync.Api.Data;

namespace StokvelSync.Api.Functions
{
    public class ReceiptFunctions
    {
        private readonly ILogger<ReceiptFunctions> _logger;

        public ReceiptFunctions(ILogger<ReceiptFunctions> logger)
        {
            _logger = logger;
        }

        [Function("UploadReceipt")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
        {
            try
            {
                var form = await req.ReadFormAsync();
                string email = form["email"]!;
                int tierBase = int.Parse(form["tier"]!);
                var file = form.Files["receipt"];

                if (file == null) return new BadRequestObjectResult("No file uploaded.");

                // 1. Math: TierBase * Current Month
                int currentMonth = DateTime.UtcNow.Month;
                decimal expectedAmount = (decimal)tierBase * currentMonth;

                // 2. Setup Azure Clients (Using connection string from local.settings.json)
                string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage")!;
                var tableServiceClient = new TableServiceClient(connectionString);
                var paymentTable = tableServiceClient.GetTableClient("Payments");
                
                var blobServiceClient = new BlobServiceClient(connectionString);
                var containerClient = blobServiceClient.GetBlobContainerClient("receipts");

                // 3. Upload photo to Blob Storage
                string blobName = $"{email}/{tierBase}_{currentMonth}_{Guid.NewGuid()}.png";
                var blobClient = containerClient.GetBlobClient(blobName);
                await blobClient.UploadAsync(file.OpenReadStream());

                // 4. Save the Pending record
                var payment = new PaymentEntity
                {
                    PartitionKey = email,
                    RowKey = $"{tierBase}_{currentMonth:D2}",
                    TierBase = tierBase,
                    MonthNumber = currentMonth,
                    AmountExpected = expectedAmount,
                    BlobUrl = blobClient.Uri.ToString(),
                    Status = "Pending"
                };

                await paymentTable.UpsertEntityAsync(payment);

                return new OkObjectResult(new { message = $"R{expectedAmount} receipt uploaded successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading receipt");
                return new BadRequestObjectResult("Error processing upload.");
            }
        }
    }
}