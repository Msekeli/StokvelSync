using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using StokvelSync.Api.Data;

namespace StokvelSync.Api.Functions;

public class ReceiptFunctions
{
    private readonly ILogger<ReceiptFunctions> _logger;
    private readonly TableClient _paymentTable;
    private readonly BlobContainerClient _containerClient;

    public ReceiptFunctions(ILogger<ReceiptFunctions> logger, TableServiceClient tableService, BlobServiceClient blobService)
    {
        _logger = logger;
        _paymentTable = tableService.GetTableClient("Payments");
        _containerClient = blobService.GetBlobContainerClient("receipts");
    }

    [Function("UploadReceipt")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
    {
        try
        {
            var form = await req.ReadFormAsync();
            string email = form["email"]!;
            int tierBase = int.Parse(form["tier"]!);
            var file = form.Files["receipt"];

            if (file == null) return new BadRequestObjectResult("No file uploaded.");

            // Math: Base * Month
            int currentMonth = DateTime.UtcNow.Month;
            decimal expected = (decimal)tierBase * currentMonth;

            await _containerClient.CreateIfNotExistsAsync();
            string fileName = $"{email}/{tierBase}_{currentMonth}_{Guid.NewGuid()}.png";
            var blobClient = _containerClient.GetBlobClient(fileName);

            using var stream = file.OpenReadStream();
            await blobClient.UploadAsync(stream);

            var payment = new PaymentEntity
            {
                PartitionKey = email,
                RowKey = $"{tierBase}_{currentMonth:D2}",
                TierBase = tierBase,
                MonthNumber = currentMonth,
                AmountExpected = expected,
                BlobUrl = blobClient.Uri.ToString(),
                Status = "Pending"
            };

            await _paymentTable.CreateIfNotExistsAsync();
            await _paymentTable.UpsertEntityAsync(payment);

            return new OkObjectResult("Receipt uploaded. Awaiting approval.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading receipt");
            return new StatusCodeResult(500);
        }
    }
}