using LedgerPulse.Domain.Common;
using LedgerPulse.Domain.Ledger;
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

    public LedgerEntryType EntryType { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public static LedgerEntry Create(DateOnly businessDate, string description, decimal amount, LedgerEntryType entryType)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Description is required.", nameof(description));
        }

        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be greater than zero.");
        }

        if (entryType is LedgerEntryType.Unknown)
        {
            throw new ArgumentException("Entry type is required.", nameof(entryType));
        }

        var entry = new LedgerEntry
        {
            Id = Guid.NewGuid(),
            BusinessDate = businessDate,
            Description = description.Trim(),
            Amount = amount,
            EntryType = entryType,
            CreatedAtUtc = DateTime.UtcNow
        };

        entry.RaiseDomainEvent(new LedgerEntryRegisteredDomainEvent(entry.Id, entry.BusinessDate, entry.Amount, entry.EntryType));
        return entry;
    }
}
