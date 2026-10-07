using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NetArcaWs.EntityFrameworkCore.Migrations.MariaDb.Migrations.Invoicing
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
                    TenantHash = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    KeyHash = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RevisionNumber = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IdempotencyKey = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Service = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Environment = table.Column<int>(type: "int", nullable: false),
                    Cuit = table.Column<long>(type: "bigint", nullable: false),
                    PointOfSale = table.Column<int>(type: "int", nullable: false),
                    VoucherType = table.Column<int>(type: "int", nullable: false),
                    VoucherNumber = table.Column<long>(type: "bigint", nullable: false),
                    RemoteRequestId = table.Column<long>(type: "bigint", nullable: true),
                    Payload = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PayloadHash = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SnapshotHash = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CanonicalVersion = table.Column<int>(type: "int", nullable: false),
                    State = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    Attempt = table.Column<int>(type: "int", nullable: false),
                    LeaseUntilMilliseconds = table.Column<long>(type: "bigint", nullable: true),
                    AuthorizationCode = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ResponseXml = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NetArcaInvoiceRevisions", x => new { x.TenantHash, x.KeyHash, x.RevisionNumber });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "NetArcaInvoices",
                columns: table => new
                {
                    TenantHash = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    KeyHash = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TenantId = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IdempotencyKey = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Service = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ServiceHash = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Environment = table.Column<int>(type: "int", nullable: false),
                    Cuit = table.Column<long>(type: "bigint", nullable: false),
                    PointOfSale = table.Column<int>(type: "int", nullable: false),
                    VoucherType = table.Column<int>(type: "int", nullable: false),
                    VoucherNumber = table.Column<long>(type: "bigint", nullable: false),
                    FiscalHash = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RemoteHash = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RemoteRequestId = table.Column<long>(type: "bigint", nullable: true),
                    Payload = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PayloadHash = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CanonicalVersion = table.Column<int>(type: "int", nullable: false),
                    State = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    Attempt = table.Column<int>(type: "int", nullable: false),
                    LeaseUntilMilliseconds = table.Column<long>(type: "bigint", nullable: true),
                    AuthorizationCode = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ResponseXml = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedUtcTicks = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NetArcaInvoices", x => new { x.TenantHash, x.KeyHash });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "NetArcaInvoiceSeriesReservations",
                columns: table => new
                {
                    SeriesHash = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TenantHash = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    KeyHash = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NetArcaInvoiceSeriesReservations", x => x.SeriesHash);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

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
