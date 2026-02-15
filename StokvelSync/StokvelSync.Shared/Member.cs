using System.ComponentModel.DataAnnotations;

namespace StokvelSync.Shared;

public class Member
{
    [Required(ErrorMessage = "Email is required to login.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Full Name and Surname are required.")]
    [MinLength(3, ErrorMessage = "Please enter your full name.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "WhatsApp number is required for the group.")]
    [Phone(ErrorMessage = "Please enter a valid phone number.")]
    public string WhatsAppNumber { get; set; } = string.Empty;

    [MinLength(1, ErrorMessage = "You must select at least one savings column.")]
    public List<int> SelectedTiers { get; set; } = new List<int>();

    public decimal TotalContribution { get; set; }
    public decimal PenaltyBalance { get; set; }
    public bool HasPaidCurrentMonth { get; set; }

    public decimal GetExpectedTotal()
    {
        int currentMonth = DateTime.Now.Month;
        return SelectedTiers.Sum(tier => (decimal)tier * currentMonth);
    }
}