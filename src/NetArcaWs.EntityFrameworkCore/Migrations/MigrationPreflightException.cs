namespace NetArcaWs.EntityFrameworkCore.Migrations;

public sealed class NetArcaWsMigrationPreflightException(NetArcaWsMigrationStatus status)
    : InvalidOperationException("The selected NetArcaWs migration modules failed preflight.")
{
    public NetArcaWsMigrationStatus Status { get; } = status ?? throw new ArgumentNullException(nameof(status));
}
