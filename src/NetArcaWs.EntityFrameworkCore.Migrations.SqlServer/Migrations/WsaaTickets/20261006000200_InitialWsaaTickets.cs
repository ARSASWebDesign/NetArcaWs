using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NetArcaWs.EntityFrameworkCore.Migrations.SqlServer.Migrations.WsaaTickets
{
    /// <inheritdoc />
    public partial class InitialWsaaTickets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.CreateTable(
                name: "NetArcaWsaaTickets",
                schema: "dbo",
                columns: table => new
                {
                    KeyHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CertificateHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Endpoint = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Service = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    State = table.Column<int>(type: "int", nullable: false),
                    OwnerId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    Fence = table.Column<long>(type: "bigint", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    LeaseUntilUtcTicks = table.Column<long>(type: "bigint", nullable: true),
                    ExpiresUtcTicks = table.Column<long>(type: "bigint", nullable: true),
                    KeyId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Nonce = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    Ciphertext = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    Tag = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    UpdatedUtcTicks = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NetArcaWsaaTickets", x => x.KeyHash);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NetArcaWsaaTickets_State_UpdatedUtcTicks",
                schema: "dbo",
                table: "NetArcaWsaaTickets",
                columns: new[] { "State", "UpdatedUtcTicks" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NetArcaWsaaTickets",
                schema: "dbo");
        }
    }
}
