namespace NetArcaWs.Taxation;

/// <summary>Explicit decimal arithmetic; rates must come from the applicable ARCA catalog/business rules.</summary>
public static class TaxCalculator
{
    public static decimal Round(decimal value, int decimals = 2)
    {
        if (decimals is < 0 or > 6) throw new ArgumentOutOfRangeException(nameof(decimals));
        return decimal.Round(value, decimals, MidpointRounding.AwayFromZero);
    }

    public static decimal Vat(decimal taxableBase, decimal ratePercent)
    {
        Nonnegative(taxableBase, nameof(taxableBase));
        Nonnegative(ratePercent, nameof(ratePercent));
        return Round(checked(taxableBase * ratePercent / 100m));
    }

    public static TaxBreakdown FromGross(decimal gross, decimal ratePercent)
    {
        Nonnegative(gross, nameof(gross));
        Nonnegative(ratePercent, nameof(ratePercent));
        var total = Round(gross);
        var net = Round(total / checked(1m + ratePercent / 100m));
        return new(net, total - net, total);
    }

    public static decimal Total(decimal net, decimal vat, decimal exempt = 0, decimal untaxed = 0, decimal tributes = 0)
    {
        Nonnegative(net, nameof(net));
        Nonnegative(vat, nameof(vat));
        Nonnegative(exempt, nameof(exempt));
        Nonnegative(untaxed, nameof(untaxed));
        Nonnegative(tributes, nameof(tributes));
        return checked(Round(net) + Round(vat) + Round(exempt) + Round(untaxed) + Round(tributes));
    }

    private static void Nonnegative(decimal value, string parameter)
    {
        if (value < 0) throw new ArgumentOutOfRangeException(parameter, "Amounts and rates must be nonnegative.");
    }
}

public sealed record TaxBreakdown(decimal Net, decimal Vat, decimal Total);
