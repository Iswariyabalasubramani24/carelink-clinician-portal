using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CareLink.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAlertsSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Alerts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    AlertType = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Urgency = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TriggeredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsAcknowledged = table.Column<bool>(type: "bit", nullable: false),
                    AcknowledgedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SnoozedUntil = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Alerts_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Alerts_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClinicAlertSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    AlertType = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DefaultUrgency = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicAlertSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClinicAlertSettings_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PatientAlertSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    AlertType = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Urgency = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsOverride = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientAlertSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientAlertSettings_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Reasonable clinic-wide alert defaults for both seeded tenants
            // (Apollo=1, Charite=2) - clinicians can override per patient later.
            migrationBuilder.InsertData(
                table: "ClinicAlertSettings",
                columns: new[] { "TenantId", "AlertType", "DefaultUrgency" },
                values: new object[,]
                {
                    { 1, "DisconnectedMonitor", "Red" },
                    { 1, "LowBattery", "Yellow" },
                    { 1, "IrregularHeartbeat", "Yellow" },
                    { 2, "DisconnectedMonitor", "Red" },
                    { 2, "LowBattery", "Yellow" },
                    { 2, "IrregularHeartbeat", "Yellow" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_PatientId_AlertType",
                table: "Alerts",
                columns: new[] { "PatientId", "AlertType" });

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_TenantId",
                table: "Alerts",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicAlertSettings_TenantId_AlertType",
                table: "ClinicAlertSettings",
                columns: new[] { "TenantId", "AlertType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PatientAlertSettings_PatientId_AlertType",
                table: "PatientAlertSettings",
                columns: new[] { "PatientId", "AlertType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Alerts");

            migrationBuilder.DropTable(
                name: "ClinicAlertSettings");

            migrationBuilder.DropTable(
                name: "PatientAlertSettings");
        }
    }
}
