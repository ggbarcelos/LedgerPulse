using LedgerPulse.Domain.Ledger.Entities;
using LedgerPulse.Domain.Ledger;
using LedgerPulse.Domain.Ledger.Events;

namespace LedgerPulse.UnitTests.Domain;

public sealed class LedgerEntryTests
{
    [Fact]
    public void Create_ShouldRaiseDomainEvent()
    {
        var businessDate = new DateOnly(2026, 3, 12);

        var entry = LedgerEntry.Create(businessDate, "Payment received", 250m, LedgerEntryType.Credit);

        var domainEvent = Assert.Single(entry.DomainEvents);
        var ledgerEvent = Assert.IsType<LedgerEntryRegisteredDomainEvent>(domainEvent);
        Assert.Equal(businessDate, ledgerEvent.BusinessDate);
        Assert.Equal(250m, ledgerEvent.Amount);
        Assert.Equal(LedgerEntryType.Credit, entry.EntryType);
        Assert.Equal(LedgerEntryType.Credit, ledgerEvent.EntryType);
    }

    [Fact]
    public void Create_ShouldRejectNonPositiveAmount()
    {
        var businessDate = new DateOnly(2026, 3, 12);

        var action = () => LedgerEntry.Create(businessDate, "Invalid", 0m, LedgerEntryType.Debit);

        Assert.Throws<ArgumentOutOfRangeException>(action);
    }
}
