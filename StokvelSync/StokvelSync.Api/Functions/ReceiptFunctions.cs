using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;
using StokvelSync.Api.Data;
using Microsoft.AspNetCore.Http; // Critical for the HttpRequest parameter
using Microsoft.AspNetCore.Mvc; // Critical for the IActionResult/ObjectResult

namespace StokvelSync.Api.Functions;

public class ReceiptFunctions
{
    private readonly MemberRepository _repository;
    private readonly BlobServiceClient _blobServiceClient;
    private const string ContainerName = "receipts";

    public ReceiptFunctions(MemberRepository repository, BlobServiceClient blobServiceClient)
    {
        _repository = repository;
        _blobServiceClient = blobServiceClient;
    }

    [Function("UploadReceipt")]
    public async Task<IActionResult> UploadReceipt(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req,
        FunctionContext executionContext)
    {
        var logger = executionContext.GetLogger("UploadReceipt");

        // In the WebApplication model, we can access 'req.Form' directly
        if (!req.HasFormContentType)
        {
            return new BadRequestObjectResult("Invalid form content.");
        }

        var form = await req.ReadFormAsync();
        var file = form.Files["receipt"];
        var email = form["email"].ToString();

        if (file == null || string.IsNullOrEmpty(email))
        {
            return new BadRequestObjectResult("Missing receipt file or email.");
        }

        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);
            await containerClient.CreateIfNotExistsAsync();

            // Create unique filename for the mobile receipt
            var fileName = $"{email.Replace("@", "_")}-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 5)}.jpg";
            var blobClient = containerClient.GetBlobClient(fileName);

            using var stream = file.OpenReadStream();
            await blobClient.UploadAsync(stream, true);

            logger.LogInformation($"Receipt saved to Blob Storage: {fileName}");

            return new OkObjectResult(new { message = "Receipt uploaded successfully", fileName });
        }
        catch (Exception ex)
        {
            logger.LogError($"Blob Upload Error: {ex.Message}");
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }
}