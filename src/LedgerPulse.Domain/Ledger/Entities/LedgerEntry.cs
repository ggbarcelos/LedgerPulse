using LedgerPulse.Domain.Common;
using LedgerPulse.Domain.Ledger.Events;

namespace LedgerPulse.Domain.Ledger.Entities;

public sealed class LedgerEntry : Entity
{
    private LedgerEntry()
    {
    }

    public Guid Id { get; private set; }

    public DateOnly BusinessDate { get; private set; }

    public string Description { get; private set; } = string.Empty;

    public decimal Amount { get; private set; }

    public string Currency { get; private set; } = string.Empty;

    public DateTime CreatedAtUtc { get; private set; }

    public static LedgerEntry Create(DateOnly businessDate, string description, decimal amount, string currency)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Description is required.", nameof(description));
        }

        if (string.IsNullOrWhiteSpace(currency) || currency.Trim().Length != 3)
        {
            throw new ArgumentException("Currency must contain 3 characters.", nameof(currency));
        }

        var entry = new LedgerEntry
        {
            Id = Guid.NewGuid(),
            BusinessDate = businessDate,
            Description = description.Trim(),
            Amount = amount,
            Currency = currency.Trim().ToUpperInvariant(),
            CreatedAtUtc = DateTime.UtcNow
        };

        entry.RaiseDomainEvent(new LedgerEntryRegisteredDomainEvent(entry.Id, entry.BusinessDate, entry.Amount, entry.Currency));
        return entry;
    }
}
