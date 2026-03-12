using System.ComponentModel.DataAnnotations;

namespace LedgerPulse.Frontend.Models;

public sealed class RegisterLedgerEntryFormModel
{
    [Required]
    [StringLength(120)]
    public string Description { get; set; } = string.Empty;

    [Range(typeof(decimal), "-999999999", "999999999")]
    public decimal Amount { get; set; }

    [Required]
    [StringLength(3, MinimumLength = 3)]
    public string Currency { get; set; } = "BRL";

    public DateOnly BusinessDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
}
