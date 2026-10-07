using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NetArcaWs.EntityFrameworkCore.Migrations.SqlServer.Migrations.InvoiceRecovery
{
    /// <inheritdoc />
    public partial class InitialInvoiceRecovery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.CreateTable(
                name: "NetArcaInvoiceRecoveryJobs",
                schema: "dbo",
                columns: table => new
                {
                    TenantHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    KeyHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Service = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    State = table.Column<int>(type: "int", nullable: false),
                    InvoiceVersion = table.Column<long>(type: "bigint", nullable: false),
                    PayloadHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CanonicalVersion = table.Column<int>(type: "int", nullable: false),
                    Environment = table.Column<int>(type: "int", nullable: false),
                    Cuit = table.Column<long>(type: "bigint", nullable: false),
                    PointOfSale = table.Column<int>(type: "int", nullable: false),
                    VoucherType = table.Column<int>(type: "int", nullable: false),
                    VoucherNumber = table.Column<long>(type: "bigint", nullable: false),
                    CredentialReference = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    Attempt = table.Column<int>(type: "int", nullable: false),
                    NextAvailableMilliseconds = table.Column<long>(type: "bigint", nullable: false),
                    LeaseUntilMilliseconds = table.Column<long>(type: "bigint", nullable: true),
                    ClaimId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    Generation = table.Column<long>(type: "bigint", nullable: false),
                    LastReason = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NetArcaInvoiceRecoveryJobs", x => new { x.TenantHash, x.KeyHash });
                });

            migrationBuilder.CreateIndex(
                name: "IX_NetArcaInvoiceRecoveryJobs_State_NextAvailableMilliseconds_LeaseUntilMilliseconds",
                schema: "dbo",
                table: "NetArcaInvoiceRecoveryJobs",
                columns: new[] { "State", "NextAvailableMilliseconds", "LeaseUntilMilliseconds" });

            migrationBuilder.CreateIndex(
                name: "IX_NetArcaInvoiceRecoveryJobs_TenantHash_Service_NextAvailableMilliseconds",
                schema: "dbo",
                table: "NetArcaInvoiceRecoveryJobs",
                columns: new[] { "TenantHash", "Service", "NextAvailableMilliseconds" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NetArcaInvoiceRecoveryJobs",
                schema: "dbo");
        }
    }
}
