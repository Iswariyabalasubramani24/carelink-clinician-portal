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

            // Reasonable clinic-wide transmission-interval default for both seeded
            // tenants (Apollo=1, Charite=2) - clinicians can override per patient later.
            migrationBuilder.InsertData(
                table: "PatientScheduleSettings",
                columns: new[] { "TenantId", "PatientId", "IntervalDays" },
                values: new object[,]
                {
                    { 1, null, 30 },
                    { 2, null, 30 }
                });

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
