using FluentAssertions;
using NetArcaWs.Taxation;
using Xunit;

namespace NetArcaWs.Tests.Taxation;

public sealed class TaxCalculatorTests
{
    [Theory]
    [InlineData(1.005, 2, 1.01)]
    [InlineData(-1.005, 2, -1.01)]
    [InlineData(2.5, 0, 3)]
    [InlineData(-2.5, 0, -3)]
    [InlineData(1.23456, 3, 1.235)]
    [InlineData(1.2345678, 6, 1.234568)]
    public void Round_uses_midpoint_away_from_zero_and_requested_scale(decimal value, int decimals, decimal expected)
    {
        TaxCalculator.Round(value, decimals).Should().Be(expected);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(7)]
    public void Round_rejects_decimal_counts_outside_supported_range(int decimals)
    {
        Action round = () => TaxCalculator.Round(1.25m, decimals);

        round.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Vat_calculates_percentage_and_rounds_to_two_decimals()
    {
        TaxCalculator.Vat(100m, 21m).Should().Be(21m);
        TaxCalculator.Vat(10m, 10.5m).Should().Be(1.05m);
        TaxCalculator.Vat(10.05m, 21m).Should().Be(2.11m);
        TaxCalculator.Vat(99.99m, 0m).Should().Be(0m);
    }

    [Theory]
    [InlineData(-0.01, 21)]
    [InlineData(100, -0.01)]
    public void Vat_rejects_negative_base_or_rate(decimal taxableBase, decimal ratePercent)
    {
        Action calculate = () => TaxCalculator.Vat(taxableBase, ratePercent);

        calculate.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(121, 21, 100, 21, 121)]
    [InlineData(100, 21, 82.64, 17.36, 100)]
    [InlineData(100, 0, 100, 0, 100)]
    [InlineData(10.01, 21, 8.27, 1.74, 10.01)]
    public void FromGross_splits_gross_into_rounded_net_and_vat(decimal gross, decimal ratePercent, decimal expectedNet, decimal expectedVat, decimal expectedTotal)
    {
        TaxBreakdown result = TaxCalculator.FromGross(gross, ratePercent);

        result.Net.Should().Be(expectedNet);
        result.Vat.Should().Be(expectedVat);
        result.Total.Should().Be(expectedTotal);
        (result.Net + result.Vat).Should().Be(result.Total);
    }

    [Theory]
    [InlineData(-1, 21)]
    [InlineData(100, -1)]
    public void FromGross_rejects_negative_gross_or_rate(decimal gross, decimal ratePercent)
    {
        Action calculate = () => TaxCalculator.FromGross(gross, ratePercent);

        calculate.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Total_sums_individually_rounded_components()
    {
        TaxCalculator.Total(10.005m, 1.005m, 0.005m, 0.005m, 0.005m).Should().Be(11.05m);
    }

    [Theory]
    [InlineData(-0.01, 0, 0, 0, 0)]
    [InlineData(0, -0.01, 0, 0, 0)]
    [InlineData(0, 0, -0.01, 0, 0)]
    [InlineData(0, 0, 0, -0.01, 0)]
    [InlineData(0, 0, 0, 0, -0.01)]
    public void Total_rejects_any_negative_component(decimal net, decimal vat, decimal exempt, decimal untaxed, decimal tributes)
    {
        Action calculate = () => TaxCalculator.Total(net, vat, exempt, untaxed, tributes);

        calculate.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Calculations_are_independent_of_current_decimal_separator_culture()
    {
        var originalCulture = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("es-AR");

            TaxCalculator.Round(1.005m, 2).Should().Be(1.01m);
            TaxCalculator.Vat(10.05m, 21m).Should().Be(2.11m);
            TaxCalculator.FromGross(100m, 21m).Should().Be(new TaxBreakdown(82.64m, 17.36m, 100m));
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = originalCulture;
        }
    }
}
