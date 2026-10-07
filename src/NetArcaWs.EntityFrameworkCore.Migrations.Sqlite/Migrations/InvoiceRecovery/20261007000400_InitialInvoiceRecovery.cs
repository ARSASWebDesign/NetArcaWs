using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NetArcaWs.EntityFrameworkCore.Migrations.Sqlite.Migrations.InvoiceRecovery
{
    /// <inheritdoc />
    public partial class InitialInvoiceRecovery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NetArcaInvoiceRecoveryJobs",
                columns: table => new
                {
                    TenantHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    KeyHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    Service = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    State = table.Column<int>(type: "INTEGER", nullable: false),
                    InvoiceVersion = table.Column<long>(type: "INTEGER", nullable: false),
                    PayloadHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    CanonicalVersion = table.Column<int>(type: "INTEGER", nullable: false),
                    Environment = table.Column<int>(type: "INTEGER", nullable: false),
                    Cuit = table.Column<long>(type: "INTEGER", nullable: false),
                    PointOfSale = table.Column<int>(type: "INTEGER", nullable: false),
                    VoucherType = table.Column<int>(type: "INTEGER", nullable: false),
                    VoucherNumber = table.Column<long>(type: "INTEGER", nullable: false),
                    CredentialReference = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    Attempt = table.Column<int>(type: "INTEGER", nullable: false),
                    NextAvailableMilliseconds = table.Column<long>(type: "INTEGER", nullable: false),
                    LeaseUntilMilliseconds = table.Column<long>(type: "INTEGER", nullable: true),
                    ClaimId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: true),
                    Generation = table.Column<long>(type: "INTEGER", nullable: false),
                    LastReason = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NetArcaInvoiceRecoveryJobs", x => new { x.TenantHash, x.KeyHash });
                });

            migrationBuilder.CreateIndex(
                name: "IX_NetArcaInvoiceRecoveryJobs_State_NextAvailableMilliseconds_LeaseUntilMilliseconds",
                table: "NetArcaInvoiceRecoveryJobs",
                columns: new[] { "State", "NextAvailableMilliseconds", "LeaseUntilMilliseconds" });

            migrationBuilder.CreateIndex(
                name: "IX_NetArcaInvoiceRecoveryJobs_TenantHash_Service_NextAvailableMilliseconds",
                table: "NetArcaInvoiceRecoveryJobs",
                columns: new[] { "TenantHash", "Service", "NextAvailableMilliseconds" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NetArcaInvoiceRecoveryJobs");
        }
    }
}
