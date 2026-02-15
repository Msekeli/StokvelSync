namespace StokvelSync.Api.Models;

public class ContributionRequest
{
    public string Email { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}