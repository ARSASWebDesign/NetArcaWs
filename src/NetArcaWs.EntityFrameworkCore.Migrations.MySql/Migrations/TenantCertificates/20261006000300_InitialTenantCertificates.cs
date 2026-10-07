using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NetArcaWs.EntityFrameworkCore.Migrations.MySql.Migrations.TenantCertificates
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
                    TenantHash = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Cuit = table.Column<long>(type: "bigint", nullable: false),
                    Environment = table.Column<int>(type: "int", nullable: false),
                    ActiveVersionId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Generation = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NetArcaCertificateSlots", x => new { x.TenantHash, x.Cuit, x.Environment });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "NetArcaCertificateVersions",
                columns: table => new
                {
                    TenantHash = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Cuit = table.Column<long>(type: "bigint", nullable: false),
                    Environment = table.Column<int>(type: "int", nullable: false),
                    VersionId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CreatedAtUtcTicks = table.Column<long>(type: "bigint", nullable: false),
                    ThumbprintSha256 = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NotBeforeUtcTicks = table.Column<long>(type: "bigint", nullable: false),
                    NotAfterUtcTicks = table.Column<long>(type: "bigint", nullable: false),
                    KeyId = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Nonce = table.Column<byte[]>(type: "longblob", nullable: false),
                    Ciphertext = table.Column<byte[]>(type: "longblob", nullable: false),
                    Tag = table.Column<byte[]>(type: "longblob", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NetArcaCertificateVersions", x => new { x.TenantHash, x.Cuit, x.Environment, x.VersionId });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_NetArcaCertificateVersions_TenantHash_Cuit_Environment_NotAf~",
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
