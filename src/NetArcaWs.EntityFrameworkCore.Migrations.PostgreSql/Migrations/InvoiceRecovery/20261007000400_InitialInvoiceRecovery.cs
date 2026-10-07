using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NetArcaWs.EntityFrameworkCore.Migrations.PostgreSql.Migrations.InvoiceRecovery
{
    /// <inheritdoc />
    public partial class InitialInvoiceRecovery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.CreateTable(
                name: "NetArcaInvoiceRecoveryJobs",
                schema: "public",
                columns: table => new
                {
                    TenantHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    KeyHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TenantId = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Service = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    State = table.Column<int>(type: "integer", nullable: false),
                    InvoiceVersion = table.Column<long>(type: "bigint", nullable: false),
                    PayloadHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CanonicalVersion = table.Column<int>(type: "integer", nullable: false),
                    Environment = table.Column<int>(type: "integer", nullable: false),
                    Cuit = table.Column<long>(type: "bigint", nullable: false),
                    PointOfSale = table.Column<int>(type: "integer", nullable: false),
                    VoucherType = table.Column<int>(type: "integer", nullable: false),
                    VoucherNumber = table.Column<long>(type: "bigint", nullable: false),
                    CredentialReference = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Attempt = table.Column<int>(type: "integer", nullable: false),
                    NextAvailableMilliseconds = table.Column<long>(type: "bigint", nullable: false),
                    LeaseUntilMilliseconds = table.Column<long>(type: "bigint", nullable: true),
                    ClaimId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    Generation = table.Column<long>(type: "bigint", nullable: false),
                    LastReason = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NetArcaInvoiceRecoveryJobs", x => new { x.TenantHash, x.KeyHash });
                });

            migrationBuilder.CreateIndex(
                name: "IX_NetArcaInvoiceRecoveryJobs_State_NextAvailableMilliseconds_~",
                schema: "public",
                table: "NetArcaInvoiceRecoveryJobs",
                columns: new[] { "State", "NextAvailableMilliseconds", "LeaseUntilMilliseconds" });

            migrationBuilder.CreateIndex(
                name: "IX_NetArcaInvoiceRecoveryJobs_TenantHash_Service_NextAvailable~",
                schema: "public",
                table: "NetArcaInvoiceRecoveryJobs",
                columns: new[] { "TenantHash", "Service", "NextAvailableMilliseconds" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NetArcaInvoiceRecoveryJobs",
                schema: "public");
        }
    }
}
