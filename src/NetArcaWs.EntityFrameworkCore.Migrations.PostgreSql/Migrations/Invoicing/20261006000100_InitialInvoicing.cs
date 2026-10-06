using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NetArcaWs.EntityFrameworkCore.Migrations.PostgreSql.Migrations.Invoicing
{
    /// <inheritdoc />
    public partial class InitialInvoicing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.CreateTable(
                name: "NetArcaInvoiceRevisions",
                schema: "public",
                columns: table => new
                {
                    TenantHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    KeyHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RevisionNumber = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Service = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Environment = table.Column<int>(type: "integer", nullable: false),
                    Cuit = table.Column<long>(type: "bigint", nullable: false),
                    PointOfSale = table.Column<int>(type: "integer", nullable: false),
                    VoucherType = table.Column<int>(type: "integer", nullable: false),
                    VoucherNumber = table.Column<long>(type: "bigint", nullable: false),
                    RemoteRequestId = table.Column<long>(type: "bigint", nullable: true),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    PayloadHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SnapshotHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CanonicalVersion = table.Column<int>(type: "integer", nullable: false),
                    State = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    Attempt = table.Column<int>(type: "integer", nullable: false),
                    LeaseUntilMilliseconds = table.Column<long>(type: "bigint", nullable: true),
                    AuthorizationCode = table.Column<string>(type: "text", nullable: true),
                    ResponseXml = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NetArcaInvoiceRevisions", x => new { x.TenantHash, x.KeyHash, x.RevisionNumber });
                });

            migrationBuilder.CreateTable(
                name: "NetArcaInvoices",
                schema: "public",
                columns: table => new
                {
                    TenantHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    KeyHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TenantId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Service = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ServiceHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Environment = table.Column<int>(type: "integer", nullable: false),
                    Cuit = table.Column<long>(type: "bigint", nullable: false),
                    PointOfSale = table.Column<int>(type: "integer", nullable: false),
                    VoucherType = table.Column<int>(type: "integer", nullable: false),
                    VoucherNumber = table.Column<long>(type: "bigint", nullable: false),
                    FiscalHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RemoteHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    RemoteRequestId = table.Column<long>(type: "bigint", nullable: true),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    PayloadHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CanonicalVersion = table.Column<int>(type: "integer", nullable: false),
                    State = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    Attempt = table.Column<int>(type: "integer", nullable: false),
                    LeaseUntilMilliseconds = table.Column<long>(type: "bigint", nullable: true),
                    AuthorizationCode = table.Column<string>(type: "text", nullable: true),
                    ResponseXml = table.Column<string>(type: "text", nullable: true),
                    CreatedUtcTicks = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NetArcaInvoices", x => new { x.TenantHash, x.KeyHash });
                });

            migrationBuilder.CreateTable(
                name: "NetArcaInvoiceSeriesReservations",
                schema: "public",
                columns: table => new
                {
                    SeriesHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TenantHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    KeyHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NetArcaInvoiceSeriesReservations", x => x.SeriesHash);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NetArcaInvoices_TenantHash_CreatedUtcTicks",
                schema: "public",
                table: "NetArcaInvoices",
                columns: new[] { "TenantHash", "CreatedUtcTicks" });

            migrationBuilder.CreateIndex(
                name: "UX_NetArcaInvoices_Fiscal",
                schema: "public",
                table: "NetArcaInvoices",
                column: "FiscalHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_NetArcaInvoices_Remote",
                schema: "public",
                table: "NetArcaInvoices",
                column: "RemoteHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NetArcaInvoiceSeriesReservations_TenantHash_KeyHash",
                schema: "public",
                table: "NetArcaInvoiceSeriesReservations",
                columns: new[] { "TenantHash", "KeyHash" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NetArcaInvoiceRevisions",
                schema: "public");

            migrationBuilder.DropTable(
                name: "NetArcaInvoices",
                schema: "public");

            migrationBuilder.DropTable(
                name: "NetArcaInvoiceSeriesReservations",
                schema: "public");
        }
    }
}
