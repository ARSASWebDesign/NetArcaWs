using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NetArcaWs.EntityFrameworkCore.Migrations.Sqlite.Migrations.Invoicing
{
    /// <inheritdoc />
    public partial class InitialInvoicing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NetArcaInvoiceRevisions",
                columns: table => new
                {
                    TenantHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    KeyHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    RevisionNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Service = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Environment = table.Column<int>(type: "INTEGER", nullable: false),
                    Cuit = table.Column<long>(type: "INTEGER", nullable: false),
                    PointOfSale = table.Column<int>(type: "INTEGER", nullable: false),
                    VoucherType = table.Column<int>(type: "INTEGER", nullable: false),
                    VoucherNumber = table.Column<long>(type: "INTEGER", nullable: false),
                    RemoteRequestId = table.Column<long>(type: "INTEGER", nullable: true),
                    Payload = table.Column<string>(type: "TEXT", nullable: false),
                    PayloadHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    SnapshotHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    CanonicalVersion = table.Column<int>(type: "INTEGER", nullable: false),
                    State = table.Column<int>(type: "INTEGER", nullable: false),
                    Version = table.Column<long>(type: "INTEGER", nullable: false),
                    Attempt = table.Column<int>(type: "INTEGER", nullable: false),
                    LeaseUntilMilliseconds = table.Column<long>(type: "INTEGER", nullable: true),
                    AuthorizationCode = table.Column<string>(type: "TEXT", nullable: true),
                    ResponseXml = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NetArcaInvoiceRevisions", x => new { x.TenantHash, x.KeyHash, x.RevisionNumber });
                });

            migrationBuilder.CreateTable(
                name: "NetArcaInvoices",
                columns: table => new
                {
                    TenantHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    KeyHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Service = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    ServiceHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Environment = table.Column<int>(type: "INTEGER", nullable: false),
                    Cuit = table.Column<long>(type: "INTEGER", nullable: false),
                    PointOfSale = table.Column<int>(type: "INTEGER", nullable: false),
                    VoucherType = table.Column<int>(type: "INTEGER", nullable: false),
                    VoucherNumber = table.Column<long>(type: "INTEGER", nullable: false),
                    FiscalHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    RemoteHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    RemoteRequestId = table.Column<long>(type: "INTEGER", nullable: true),
                    Payload = table.Column<string>(type: "TEXT", nullable: false),
                    PayloadHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    CanonicalVersion = table.Column<int>(type: "INTEGER", nullable: false),
                    State = table.Column<int>(type: "INTEGER", nullable: false),
                    Version = table.Column<long>(type: "INTEGER", nullable: false),
                    Attempt = table.Column<int>(type: "INTEGER", nullable: false),
                    LeaseUntilMilliseconds = table.Column<long>(type: "INTEGER", nullable: true),
                    AuthorizationCode = table.Column<string>(type: "TEXT", nullable: true),
                    ResponseXml = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedUtcTicks = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NetArcaInvoices", x => new { x.TenantHash, x.KeyHash });
                });

            migrationBuilder.CreateTable(
                name: "NetArcaInvoiceSeriesReservations",
                columns: table => new
                {
                    SeriesHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    TenantHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    KeyHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NetArcaInvoiceSeriesReservations", x => x.SeriesHash);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NetArcaInvoices_TenantHash_CreatedUtcTicks",
                table: "NetArcaInvoices",
                columns: new[] { "TenantHash", "CreatedUtcTicks" });

            migrationBuilder.CreateIndex(
                name: "UX_NetArcaInvoices_Fiscal",
                table: "NetArcaInvoices",
                column: "FiscalHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_NetArcaInvoices_Remote",
                table: "NetArcaInvoices",
                column: "RemoteHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NetArcaInvoiceSeriesReservations_TenantHash_KeyHash",
                table: "NetArcaInvoiceSeriesReservations",
                columns: new[] { "TenantHash", "KeyHash" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NetArcaInvoiceRevisions");

            migrationBuilder.DropTable(
                name: "NetArcaInvoices");

            migrationBuilder.DropTable(
                name: "NetArcaInvoiceSeriesReservations");
        }
    }
}
