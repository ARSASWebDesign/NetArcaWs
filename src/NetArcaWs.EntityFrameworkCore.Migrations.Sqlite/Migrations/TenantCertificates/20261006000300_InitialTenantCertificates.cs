using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NetArcaWs.EntityFrameworkCore.Migrations.Sqlite.Migrations.TenantCertificates
{
    /// <inheritdoc />
    public partial class InitialTenantCertificates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NetArcaCertificateSlots",
                columns: table => new
                {
                    TenantHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Cuit = table.Column<long>(type: "INTEGER", nullable: false),
                    Environment = table.Column<int>(type: "INTEGER", nullable: false),
                    ActiveVersionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Generation = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NetArcaCertificateSlots", x => new { x.TenantHash, x.Cuit, x.Environment });
                });

            migrationBuilder.CreateTable(
                name: "NetArcaCertificateVersions",
                columns: table => new
                {
                    TenantHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Cuit = table.Column<long>(type: "INTEGER", nullable: false),
                    Environment = table.Column<int>(type: "INTEGER", nullable: false),
                    VersionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAtUtcTicks = table.Column<long>(type: "INTEGER", nullable: false),
                    ThumbprintSha256 = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    NotBeforeUtcTicks = table.Column<long>(type: "INTEGER", nullable: false),
                    NotAfterUtcTicks = table.Column<long>(type: "INTEGER", nullable: false),
                    KeyId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Nonce = table.Column<byte[]>(type: "BLOB", nullable: false),
                    Ciphertext = table.Column<byte[]>(type: "BLOB", nullable: false),
                    Tag = table.Column<byte[]>(type: "BLOB", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NetArcaCertificateVersions", x => new { x.TenantHash, x.Cuit, x.Environment, x.VersionId });
                });

            migrationBuilder.CreateIndex(
                name: "IX_NetArcaCertificateVersions_TenantHash_Cuit_Environment_NotAfterUtcTicks",
                table: "NetArcaCertificateVersions",
                columns: new[] { "TenantHash", "Cuit", "Environment", "NotAfterUtcTicks" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NetArcaCertificateSlots");

            migrationBuilder.DropTable(
                name: "NetArcaCertificateVersions");
        }
    }
}
