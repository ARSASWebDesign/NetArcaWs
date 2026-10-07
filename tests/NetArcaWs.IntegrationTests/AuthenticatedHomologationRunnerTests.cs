using Xunit;

namespace NetArcaWs.IntegrationTests;

public sealed class AuthenticatedHomologationRunnerTests
{
    [Fact]
    public void Complete_mode_contains_exactly_the_nine_authenticated_read_selectors()
    {
        Assert.Equal(
            ["wsfe", "wsfex", "wsmtxca", "wscdc", "wsfecred", "padron-a4", "padron-a5", "padron-a10", "padron-a13"],
            AuthenticatedHomologationRunner.CompleteReadSelectors);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("100000000")]
    [InlineData("1.5")]
    public void Invalid_explicit_B_or_C_number_is_rejected_before_authenticated_setup(string? value)
    {
        Assert.Throws<InvalidOperationException>(() => HomologationInputValidation.ReadPositive(
            "ARCA_HOMOLOGY_VOUCHER_NUMBER_B", value, 99999999));
        Assert.Throws<InvalidOperationException>(() => HomologationInputValidation.ReadPositive(
            "ARCA_HOMOLOGY_VOUCHER_NUMBER_C", value, 99999999));
    }

    [Theory]
    [InlineData("1", 1)]
    [InlineData("99999999", 99999999)]
    public void Explicit_B_and_C_numbers_accept_only_the_supported_range(string value, long expected)
    {
        Assert.Equal(expected, HomologationInputValidation.ReadPositive("B", value, 99999999));
        Assert.Equal(expected, HomologationInputValidation.ReadPositive("C", value, 99999999));
    }

    [Fact]
    public async Task A_failed_read_selector_does_not_skip_the_next_and_is_returned_for_overall_failure()
    {
        List<string> attempted = [];
        List<string> reported = [];
        string[] failures = (await AuthenticatedHomologationRunner.RunIndependentAsync(
            ["wsfe", "wsfex", "wsmtxca"],
            selector =>
            {
                attempted.Add(selector);
                return selector == "wsfe" ? Task.FromException(new InvalidOperationException("synthetic")) : Task.CompletedTask;
            },
            (selector, _) =>
            {
                reported.Add(selector);
                return Task.CompletedTask;
            },
            CancellationToken.None)).ToArray();

        Assert.Equal(["wsfe", "wsfex", "wsmtxca"], attempted);
        Assert.Equal(["wsfe"], reported);
        Assert.Equal(["wsfe"], failures);
    }

    [Fact]
    public async Task Caller_cancellation_stops_the_remaining_selectors()
    {
        using var cancellation = new CancellationTokenSource();
        List<string> attempted = [];
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => AuthenticatedHomologationRunner.RunIndependentAsync(
            ["wsfe", "wsfex"],
            selector =>
            {
                attempted.Add(selector);
                cancellation.Cancel();
                return Task.FromCanceled(cancellation.Token);
            },
            (_, _) => Task.CompletedTask,
            cancellation.Token));

        Assert.Equal(["wsfe"], attempted);
    }
}
