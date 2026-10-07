using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NetArcaWs.EntityFrameworkCore.Migrations.PostgreSql.Migrations.TenantCertificates
{
    /// <inheritdoc />
    public partial class InitialTenantCertificates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.CreateTable(
                name: "NetArcaCertificateSlots",
                schema: "public",
                columns: table => new
                {
                    TenantHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Cuit = table.Column<long>(type: "bigint", nullable: false),
                    Environment = table.Column<int>(type: "integer", nullable: false),
                    ActiveVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Generation = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NetArcaCertificateSlots", x => new { x.TenantHash, x.Cuit, x.Environment });
                });

            migrationBuilder.CreateTable(
                name: "NetArcaCertificateVersions",
                schema: "public",
                columns: table => new
                {
                    TenantHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Cuit = table.Column<long>(type: "bigint", nullable: false),
                    Environment = table.Column<int>(type: "integer", nullable: false),
                    VersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtcTicks = table.Column<long>(type: "bigint", nullable: false),
                    ThumbprintSha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    NotBeforeUtcTicks = table.Column<long>(type: "bigint", nullable: false),
                    NotAfterUtcTicks = table.Column<long>(type: "bigint", nullable: false),
                    KeyId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Nonce = table.Column<byte[]>(type: "bytea", nullable: false),
                    Ciphertext = table.Column<byte[]>(type: "bytea", nullable: false),
                    Tag = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NetArcaCertificateVersions", x => new { x.TenantHash, x.Cuit, x.Environment, x.VersionId });
                });

            migrationBuilder.CreateIndex(
                name: "IX_NetArcaCertificateVersions_TenantHash_Cuit_Environment_NotA~",
                schema: "public",
                table: "NetArcaCertificateVersions",
                columns: new[] { "TenantHash", "Cuit", "Environment", "NotAfterUtcTicks" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NetArcaCertificateSlots",
                schema: "public");

            migrationBuilder.DropTable(
                name: "NetArcaCertificateVersions",
                schema: "public");
        }
    }
}
