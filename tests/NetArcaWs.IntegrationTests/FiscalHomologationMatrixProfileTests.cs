using AwesomeAssertions;
using Xunit;

namespace NetArcaWs.IntegrationTests;

public sealed class FiscalHomologationMatrixProfileTests
{
    [Fact]
    public void Matrix_contains_all_and_only_the_18_supported_named_cases_with_invoice_associations()
    {
        FiscalHomologationMatrixProfile.AllScenarios.Should().HaveCount(18);
        FiscalHomologationMatrixProfile.AllScenarios.Select(x => x.Name).Should().OnlyHaveUniqueItems();
        FiscalHomologationMatrixProfile.AllScenarios.Where(x => x.VoucherType is 2 or 3 or 7 or 8 or 12 or 13 or 20 or 21)
            .Should().OnlyContain(x => x.OriginalScenarioName != null);
        FiscalHomologationMatrixProfile.AllScenarios.Where(x => x.VoucherType is 1 or 6 or 11 or 19)
            .Should().OnlyContain(x => x.OriginalScenarioName == null);
    }

    [Fact]
    public void Preflight_accepts_fixed_modes_and_rejects_unreviewed_or_incomplete_matrix_configuration()
    {
        FiscalHomologationMatrixProfile.ValidatePreflight("consultas", "");
        FiscalHomologationMatrixProfile.ValidatePreflight("emision", "stable-run");
        FiscalHomologationMatrixProfile.ValidatePreflight("completa", "stable-run");
        Assert.Throws<InvalidOperationException>(() => FiscalHomologationMatrixProfile.ValidatePreflight("other", "stable-run"));
        Assert.Throws<InvalidOperationException>(() => FiscalHomologationMatrixProfile.ValidatePreflight("emision", ""));
    }

    [Fact]
    public void Synthetic_export_fixture_uses_separate_country_fields_and_frozen_payment_date()
    {
        DateOnly paymentDate = new(2030, 1, 11);
        FiscalExportFixture fixture = FiscalHomologationMatrixProfile.CreateExportFixture(paymentDate);
        fixture.Validate();
        fixture.DestinationCountryCode.Should().Be(203);
        fixture.CustomerCountryTaxCode.Should().Be(50_000_000_016);
        fixture.PaymentDate.Should().Be(paymentDate);
        fixture.CurrencyRate.Should().Be(1m);
        fixture.Items.Should().ContainSingle(item => item.Quantity * item.UnitPrice - item.Discount == 100m);
    }
}
