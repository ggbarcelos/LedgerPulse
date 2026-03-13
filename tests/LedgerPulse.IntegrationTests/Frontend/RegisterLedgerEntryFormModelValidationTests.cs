using System.ComponentModel.DataAnnotations;
using System.Globalization;
using LedgerPulse.Domain.Ledger;
using LedgerPulse.Frontend.Models;

namespace LedgerPulse.IntegrationTests.Frontend;

public sealed class RegisterLedgerEntryFormModelValidationTests
{
    [Fact]
    public void Validate_ShouldRejectZeroAmount_WhenCultureIsPtBr()
    {
        using var cultureScope = new CultureScope("pt-BR");
        var model = new RegisterLedgerEntryFormModel
        {
            Description = "Lancamento invalido",
            Amount = 0m,
            EntryType = LedgerEntryType.Credit,
            BusinessDate = new DateOnly(2026, 3, 13)
        };

        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(model, new ValidationContext(model), validationResults, validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(validationResults, result => result.ErrorMessage == "O valor deve ser maior que zero.");
    }

    [Fact]
    public void Validate_ShouldAcceptPositiveAmount_WhenCultureIsPtBr()
    {
        using var cultureScope = new CultureScope("pt-BR");
        var model = new RegisterLedgerEntryFormModel
        {
            Description = "Lancamento valido",
            Amount = 123.45m,
            EntryType = LedgerEntryType.Debit,
            BusinessDate = new DateOnly(2026, 3, 13)
        };

        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(model, new ValidationContext(model), validationResults, validateAllProperties: true);

        Assert.True(isValid);
        Assert.Empty(validationResults);
    }

    private sealed class CultureScope : IDisposable
    {
        private readonly CultureInfo _originalCulture = CultureInfo.CurrentCulture;
        private readonly CultureInfo _originalUiCulture = CultureInfo.CurrentUICulture;

        public CultureScope(string cultureName)
        {
            var culture = CultureInfo.GetCultureInfo(cultureName);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
        }

        public void Dispose()
        {
            CultureInfo.CurrentCulture = _originalCulture;
            CultureInfo.CurrentUICulture = _originalUiCulture;
        }
    }
}
