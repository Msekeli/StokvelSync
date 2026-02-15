using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure.Data.Tables;
using StokvelSync.Api.Data;
using System.Text.Json;

namespace StokvelSync.Api.Functions;

public class AdminFunctions
{
    private readonly ILogger<AdminFunctions> _logger;
    private readonly TableClient _memberTable;
    private readonly TableClient _paymentTable;

    public AdminFunctions(ILoggerFactory loggerFactory, TableServiceClient tableService)
    {
        _logger = loggerFactory.CreateLogger<AdminFunctions>();
        _memberTable = tableService.GetTableClient("Members");
        _paymentTable = tableService.GetTableClient("Payments");
    }

    [Function("ApprovePayment")]
    public async Task<IActionResult> Approve(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
    {
        try
        {
            // 1. Parse the request (Expected JSON: { "email": "...", "paymentKey": "..." })
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonSerializer.Deserialize<ApprovalRequest>(requestBody);

            if (data == null || string.IsNullOrEmpty(data.Email) || string.IsNullOrEmpty(data.PaymentKey))
                return new BadRequestObjectResult("Invalid approval data.");

            // 2. Fetch the Payment and the Member
            var paymentResponse = await _paymentTable.GetEntityAsync<PaymentEntity>(data.Email, data.PaymentKey);
            var memberResponse = await _memberTable.GetEntityAsync<MemberEntity>("StokvelMember", data.Email);

            var payment = paymentResponse.Value;
            var member = memberResponse.Value;

            if (payment.Status == "Approved")
                return new BadRequestObjectResult("This payment is already approved.");

            // 3. Update the Status
            payment.Status = "Approved";
            
            // 4. Update Member's Total Contribution
         // Explicitly cast decimal to double to allow the += operator to work
            member.TotalContribution += (double)payment.AmountExpected;

            // 5. Save changes to Table Storage
            await _paymentTable.UpdateEntityAsync(payment, payment.ETag);
            await _memberTable.UpdateEntityAsync(member, member.ETag);

            _logger.LogInformation($"Admin approved {payment.AmountExpected} for {data.Email}");

            return new OkObjectResult("Payment approved successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving payment");
            return new StatusCodeResult(500);
        }
    }
}

// Simple DTO for the approval request
public class ApprovalRequest
{
    public string Email { get; set; } = string.Empty;
    public string PaymentKey { get; set; } = string.Empty; // This is the RowKey (e.g., "100_03")
}