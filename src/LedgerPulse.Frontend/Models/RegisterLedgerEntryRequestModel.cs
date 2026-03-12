namespace LedgerPulse.Frontend.Models;

public sealed record RegisterLedgerEntryRequestModel(string Description, decimal Amount, string Currency, DateOnly BusinessDate);
