using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NetArcaWs.EntityFrameworkCore.Migrations.MariaDb.Migrations.WsaaTickets
{
    /// <inheritdoc />
    public partial class InitialWsaaTickets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NetArcaWsaaTickets",
                columns: table => new
                {
                    KeyHash = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CertificateHash = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Endpoint = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Service = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    State = table.Column<int>(type: "int", nullable: false),
                    OwnerId = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Fence = table.Column<long>(type: "bigint", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    LeaseUntilUtcTicks = table.Column<long>(type: "bigint", nullable: true),
                    ExpiresUtcTicks = table.Column<long>(type: "bigint", nullable: true),
                    KeyId = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Nonce = table.Column<byte[]>(type: "longblob", nullable: true),
                    Ciphertext = table.Column<byte[]>(type: "longblob", nullable: true),
                    Tag = table.Column<byte[]>(type: "longblob", nullable: true),
                    UpdatedUtcTicks = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NetArcaWsaaTickets", x => x.KeyHash);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_NetArcaWsaaTickets_State_UpdatedUtcTicks",
                table: "NetArcaWsaaTickets",
                columns: new[] { "State", "UpdatedUtcTicks" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NetArcaWsaaTickets");
        }
    }
}
