using System.ComponentModel.DataAnnotations;
using LedgerPulse.Domain.Ledger;
using LedgerPulse.Frontend.Validation;

namespace LedgerPulse.Frontend.Models;

public sealed class RegisterLedgerEntryFormModel
{
    [Required(ErrorMessage = "A descricao e obrigatoria.")]
    [StringLength(120, ErrorMessage = "A descricao deve ter no maximo 120 caracteres.")]
    public string Description { get; set; } = string.Empty;

    [PositiveAmount(ErrorMessage = "O valor deve ser maior que zero.")]
    public decimal Amount { get; set; }

    public LedgerEntryType EntryType { get; set; } = LedgerEntryType.Credit;

    public DateOnly BusinessDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
}
