using System.ComponentModel.DataAnnotations;

namespace LedgerPulse.Frontend.Validation;

[AttributeUsage(AttributeTargets.Property)]
public sealed class PositiveAmountAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
        => value is decimal amount && amount > 0m;
}
