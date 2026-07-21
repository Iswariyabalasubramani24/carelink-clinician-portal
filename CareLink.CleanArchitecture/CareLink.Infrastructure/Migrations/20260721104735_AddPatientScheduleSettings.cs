using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CareLink.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPatientScheduleSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PatientScheduleSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    PatientId = table.Column<int>(type: "int", nullable: true),
                    IntervalDays = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientScheduleSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientScheduleSettings_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PatientScheduleSettings_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Reasonable clinic-wide transmission-interval default for the
            // dev-seeded tenants (Apollo=1, Charite=2). Guarded per tenant: on a
            // fresh (e.g. production) database these tenants don't exist, and
            // new tenants get their defaults from the hospital-provisioning flow.
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM Tenants WHERE Id = 1)
    INSERT INTO PatientScheduleSettings (TenantId, PatientId, IntervalDays) VALUES (1, NULL, 30);
IF EXISTS (SELECT 1 FROM Tenants WHERE Id = 2)
    INSERT INTO PatientScheduleSettings (TenantId, PatientId, IntervalDays) VALUES (2, NULL, 30);");

            migrationBuilder.CreateIndex(
                name: "IX_PatientScheduleSettings_PatientId",
                table: "PatientScheduleSettings",
                column: "PatientId",
                unique: true,
                filter: "[PatientId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PatientScheduleSettings_TenantId",
                table: "PatientScheduleSettings",
                column: "TenantId",
                unique: true,
                filter: "[TenantId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PatientScheduleSettings");
        }
    }
}
