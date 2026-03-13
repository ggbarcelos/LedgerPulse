using System.Text.Json.Serialization;

namespace LedgerPulse.Domain.Ledger;

[JsonConverter(typeof(JsonStringEnumConverter<LedgerEntryType>))]
public enum LedgerEntryType
{
    Unknown = 0,
    Credit = 1,
    Debit = 2
}
