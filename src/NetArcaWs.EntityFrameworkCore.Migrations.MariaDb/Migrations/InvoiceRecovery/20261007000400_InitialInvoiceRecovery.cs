using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NetArcaWs.EntityFrameworkCore.Migrations.MariaDb.Migrations.InvoiceRecovery
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
                    TenantHash = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    KeyHash = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TenantId = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IdempotencyKey = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Service = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    State = table.Column<int>(type: "int", nullable: false),
                    InvoiceVersion = table.Column<long>(type: "bigint", nullable: false),
                    PayloadHash = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CanonicalVersion = table.Column<int>(type: "int", nullable: false),
                    Environment = table.Column<int>(type: "int", nullable: false),
                    Cuit = table.Column<long>(type: "bigint", nullable: false),
                    PointOfSale = table.Column<int>(type: "int", nullable: false),
                    VoucherType = table.Column<int>(type: "int", nullable: false),
                    VoucherNumber = table.Column<long>(type: "bigint", nullable: false),
                    CredentialReference = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Attempt = table.Column<int>(type: "int", nullable: false),
                    NextAvailableMilliseconds = table.Column<long>(type: "bigint", nullable: false),
                    LeaseUntilMilliseconds = table.Column<long>(type: "bigint", nullable: true),
                    ClaimId = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Generation = table.Column<long>(type: "bigint", nullable: false),
                    LastReason = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NetArcaInvoiceRecoveryJobs", x => new { x.TenantHash, x.KeyHash });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_NetArcaInvoiceRecoveryJobs_State_NextAvailableMilliseconds_L~",
                table: "NetArcaInvoiceRecoveryJobs",
                columns: new[] { "State", "NextAvailableMilliseconds", "LeaseUntilMilliseconds" });

            migrationBuilder.CreateIndex(
                name: "IX_NetArcaInvoiceRecoveryJobs_TenantHash_Service_NextAvailableM~",
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
