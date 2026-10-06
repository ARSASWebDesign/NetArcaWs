using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NetArcaWs.EntityFrameworkCore.Migrations.Sqlite.Migrations.WsaaTickets
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
                    KeyHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    CertificateHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Endpoint = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    Service = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    State = table.Column<int>(type: "INTEGER", nullable: false),
                    OwnerId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: true),
                    Fence = table.Column<long>(type: "INTEGER", nullable: false),
                    Version = table.Column<long>(type: "INTEGER", nullable: false),
                    LeaseUntilUtcTicks = table.Column<long>(type: "INTEGER", nullable: true),
                    ExpiresUtcTicks = table.Column<long>(type: "INTEGER", nullable: true),
                    KeyId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    Nonce = table.Column<byte[]>(type: "BLOB", nullable: true),
                    Ciphertext = table.Column<byte[]>(type: "BLOB", nullable: true),
                    Tag = table.Column<byte[]>(type: "BLOB", nullable: true),
                    UpdatedUtcTicks = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NetArcaWsaaTickets", x => x.KeyHash);
                });

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
