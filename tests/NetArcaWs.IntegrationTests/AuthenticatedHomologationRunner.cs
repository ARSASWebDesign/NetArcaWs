namespace NetArcaWs.IntegrationTests;

/// <summary>Runs independent authenticated selectors and reports every failed selector after trying the rest.</summary>
internal static class AuthenticatedHomologationRunner
{
    public static IReadOnlyList<string> CompleteReadSelectors { get; } = Array.AsReadOnly(new[]
        { "wsfe", "wsfex", "wsmtxca", "wscdc", "wsfecred", "padron-a4", "padron-a5", "padron-a10", "padron-a13" });

    public static async Task<IReadOnlyList<string>> RunIndependentAsync(
        IEnumerable<string> selectors,
        Func<string, Task> runSelector,
        Func<string, Exception, Task> reportFailure,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(selectors);
        ArgumentNullException.ThrowIfNull(runSelector);
        ArgumentNullException.ThrowIfNull(reportFailure);

        List<string> failures = [];
        foreach (string selector in selectors)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await runSelector(selector).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                failures.Add(selector);
                await reportFailure(selector, exception).ConfigureAwait(false);
            }
        }
        return failures;
    }
}
