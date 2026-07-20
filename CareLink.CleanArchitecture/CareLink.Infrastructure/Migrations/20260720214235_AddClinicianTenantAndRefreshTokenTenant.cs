using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CareLink.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClinicianTenantAndRefreshTokenTenant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Existing refresh tokens predate the notion of an "active tenant"
            // and would otherwise get TenantId=0, which violates the new FK
            // to Tenants below. These are just old dev sessions - clearing
            // them just means those sessions need to log in again.
            migrationBuilder.Sql("DELETE FROM RefreshTokens");

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "RefreshTokens",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ClinicianTenants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClinicianId = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicianTenants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClinicianTenants_Clinicians_ClinicianId",
                        column: x => x.ClinicianId,
                        principalTable: "Clinicians",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClinicianTenants_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_TenantId",
                table: "RefreshTokens",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicianTenants_ClinicianId_TenantId",
                table: "ClinicianTenants",
                columns: new[] { "ClinicianId", "TenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClinicianTenants_TenantId",
                table: "ClinicianTenants",
                column: "TenantId");

            migrationBuilder.AddForeignKey(
                name: "FK_RefreshTokens_Tenants_TenantId",
                table: "RefreshTokens",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RefreshTokens_Tenants_TenantId",
                table: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "ClinicianTenants");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_TenantId",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "RefreshTokens");
        }
    }
}
