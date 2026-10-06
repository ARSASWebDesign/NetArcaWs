using NetArcaWs.Tool;

using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cancellation.Cancel(); };
try
{
    return await new CertificateTool(Console.Out, Console.Error, Environment.GetEnvironmentVariable)
        .RunAsync(args, cancellation.Token);
}
catch (OperationCanceledException)
{
    Console.Error.WriteLine("Operación cancelada.");
    return 130;
}
