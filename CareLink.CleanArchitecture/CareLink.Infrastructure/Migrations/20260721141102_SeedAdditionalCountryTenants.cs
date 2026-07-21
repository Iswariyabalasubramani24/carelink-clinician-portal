using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CareLink.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedAdditionalCountryTenants : Migration
    {
        // Pre-computed BCrypt hashes (work factor 11) so this seed is deterministic
        // and reproducible on any fresh database. BCrypt salts are random per call,
        // so the hash is baked in here rather than generated at migration time.
        //   DoctorHash -> "Test@123"   AdminHash -> "Admin@123"
        private const string DoctorHash = "$2a$11$.26ye0whnQDw5wA7E55cPO6gPabEqQ9zvA5fQ8qO2hUmfAa5D9syG";
        private const string AdminHash = "$2a$11$Q7W8jsFjJYBlRbMWDTTnH.JvryntYZcSSt6wznKQE.vbr/tpxTida";

        // Regions this migration owns; Down() removes exactly these.
        private static readonly string[] SeededRegions =
            { "France", "Spain", "United States", "United Kingdom", "Canada" };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Two Romance-language hospitals so the existing fr/es UI translations
            // each have a real country to select on the login screen.
            SeedTenant(migrationBuilder,
                hospital: "Pompidou Hospital", region: "France", lang: "fr",
                docEmail: "doctor@pompidou.fr", docFirst: "Camille", docLast: "Dubois",
                admEmail: "admin@pompidou.fr", admFirst: "Etienne", admLast: "Moreau",
                patients:
                    "(@t, N'PMP-1001', N'Emile', N'Rousseau', '1968-06-11', '+33-1-23456789', 'emile.rousseau@example.com', 'ICD', N'Medtronic', N'Evera XT', 'MDT-ICD-FR001', '2022-09-10', 88.0, 71, DATEADD(day, -2, GETUTCDATE()), 1, GETUTCDATE()), " +
                    "(@t, N'PMP-1002', N'Camille', N'Petit', '1975-02-23', '+33-1-98765432', 'camille.petit@example.com', 'Pacemaker', N'Boston Scientific', N'Accolade MRI', 'BSX-PM-FR002', '2021-04-18', 91.0, 69, DATEADD(day, -5, GETUTCDATE()), 1, GETUTCDATE())");

            SeedTenant(migrationBuilder,
                hospital: "La Paz Hospital", region: "Spain", lang: "es",
                docEmail: "doctor@lapaz.es", docFirst: "Lucia", docLast: "Fernandez",
                admEmail: "admin@lapaz.es", admFirst: "Mateo", admLast: "Garcia",
                patients:
                    "(@t, N'LPZ-1001', N'Lucia', N'Martinez', '1970-11-05', '+34-91-1234567', 'lucia.martinez@example.com', 'ICD', N'Abbott', N'Gallant HF', 'ABT-ICD-ES001', '2023-01-22', 85.0, 73, DATEADD(day, -1, GETUTCDATE()), 1, GETUTCDATE()), " +
                    "(@t, N'LPZ-1002', N'Javier', N'Sanchez', '1982-08-30', '+34-91-7654321', 'javier.sanchez@example.com', 'ICM', N'Biotronik', N'BioMonitor III', 'BTK-ICM-ES002', '2023-07-14', 96.0, 66, DATEADD(day, -3, GETUTCDATE()), 1, GETUTCDATE())");

            // Three English-speaking hospitals. LanguageCode 'en' reuses the default
            // (fully translated) UI, so no new i18n resources are required.
            SeedTenant(migrationBuilder,
                hospital: "Mercy General Hospital", region: "United States", lang: "en",
                docEmail: "doctor@mercygeneral.us", docFirst: "Robert", docLast: "Johnson",
                admEmail: "admin@mercygeneral.us", admFirst: "Mary", admLast: "Williams",
                patients:
                    "(@t, N'MGH-1001', N'Robert', N'Johnson', '1960-03-15', '+1-212-5550101', 'robert.johnson@example.com', 'ICD', N'Medtronic', N'Cobalt XT', 'MDT-ICD-US001', '2022-05-20', 84.0, 75, DATEADD(day, -1, GETUTCDATE()), 1, GETUTCDATE()), " +
                    "(@t, N'MGH-1002', N'Mary', N'Williams', '1978-07-08', '+1-212-5550102', 'mary.williams@example.com', 'CRT_D', N'Boston Scientific', N'Resonate', 'BSX-CRT-US002', '2021-12-03', 90.0, 70, DATEADD(day, -4, GETUTCDATE()), 1, GETUTCDATE())");

            SeedTenant(migrationBuilder,
                hospital: "St. Thomas Hospital", region: "United Kingdom", lang: "en",
                docEmail: "doctor@stthomas.uk", docFirst: "William", docLast: "Brown",
                admEmail: "admin@stthomas.uk", admFirst: "Sophie", admLast: "Taylor",
                patients:
                    "(@t, N'STH-1001', N'William', N'Brown', '1955-09-19', '+44-20-79460101', 'william.brown@example.com', 'Pacemaker', N'Abbott', N'Assurity MRI', 'ABT-PM-UK001', '2023-02-11', 93.0, 67, DATEADD(day, -2, GETUTCDATE()), 1, GETUTCDATE()), " +
                    "(@t, N'STH-1002', N'Sophie', N'Taylor', '1969-12-30', '+44-20-79460102', 'sophie.taylor@example.com', 'ICM', N'Medtronic', N'Reveal LINQ', 'MDT-ICM-UK002', '2023-06-25', 97.0, 65, DATEADD(day, -6, GETUTCDATE()), 1, GETUTCDATE())");

            SeedTenant(migrationBuilder,
                hospital: "Toronto General Hospital", region: "Canada", lang: "en",
                docEmail: "doctor@torontogeneral.ca", docFirst: "David", docLast: "Wilson",
                admEmail: "admin@torontogeneral.ca", admFirst: "Emma", admLast: "Martin",
                patients:
                    "(@t, N'TGH-1001', N'David', N'Wilson', '1962-04-27', '+1-416-5550101', 'david.wilson@example.com', 'ICD', N'Biotronik', N'Rivacor 7', 'BTK-ICD-CA001', '2022-08-14', 86.0, 72, DATEADD(day, -1, GETUTCDATE()), 1, GETUTCDATE()), " +
                    "(@t, N'TGH-1002', N'Emma', N'Martin', '1980-01-16', '+1-416-5550102', 'emma.martin@example.com', 'Pacemaker', N'Boston Scientific', N'Accolade MRI', 'BSX-PM-CA002', '2021-10-09', 89.0, 68, DATEADD(day, -3, GETUTCDATE()), 1, GETUTCDATE())");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var regionList = string.Join(", ", System.Array.ConvertAll(SeededRegions, r => $"N'{r}'"));

            // Remove all rows belonging to the seeded tenants, children first to
            // respect foreign keys, then the tenants themselves.
            migrationBuilder.Sql($@"
DECLARE @ids TABLE (Id INT);
INSERT INTO @ids SELECT Id FROM Tenants WHERE Region IN ({regionList});

DELETE FROM PatientScheduleSettings WHERE TenantId IN (SELECT Id FROM @ids);
DELETE FROM ReportSettings          WHERE TenantId IN (SELECT Id FROM @ids);
DELETE FROM ClinicAlertSettings     WHERE TenantId IN (SELECT Id FROM @ids);
DELETE FROM Patients                WHERE TenantId IN (SELECT Id FROM @ids);
DELETE FROM ClinicianTenants        WHERE TenantId IN (SELECT Id FROM @ids);
DELETE FROM RefreshTokens           WHERE TenantId IN (SELECT Id FROM @ids);
DELETE FROM Clinicians              WHERE TenantId IN (SELECT Id FROM @ids);
DELETE FROM Tenants                 WHERE Id IN (SELECT Id FROM @ids);");
        }

        // Emits one self-contained batch: a tenant, its doctor + admin clinicians,
        // the clinician-tenant access rows, clinic-default alert/schedule/report
        // settings, and two sample patients. SCOPE_IDENTITY() threads the newly
        // generated identity keys through, so no IDs are hard-coded.
        private static void SeedTenant(
            MigrationBuilder migrationBuilder,
            string hospital, string region, string lang,
            string docEmail, string docFirst, string docLast,
            string admEmail, string admFirst, string admLast,
            string patients)
        {
            migrationBuilder.Sql($@"
DECLARE @t INT, @doc INT, @adm INT;

INSERT INTO Tenants (Name, Region, LanguageCode, IsActive, CreatedAt)
VALUES (N'{hospital}', N'{region}', '{lang}', 1, GETUTCDATE());
SET @t = SCOPE_IDENTITY();

INSERT INTO Clinicians (TenantId, Email, PasswordHash, FirstName, LastName, Role, LanguageCode, IsActive, CreatedAt)
VALUES (@t, '{docEmail}', '{DoctorHash}', N'{docFirst}', N'{docLast}', 'Clinician', '{lang}', 1, GETUTCDATE());
SET @doc = SCOPE_IDENTITY();

INSERT INTO Clinicians (TenantId, Email, PasswordHash, FirstName, LastName, Role, LanguageCode, IsActive, CreatedAt)
VALUES (@t, '{admEmail}', '{AdminHash}', N'{admFirst}', N'{admLast}', 'Admin', '{lang}', 1, GETUTCDATE());
SET @adm = SCOPE_IDENTITY();

INSERT INTO ClinicianTenants (ClinicianId, TenantId) VALUES (@doc, @t), (@adm, @t);

INSERT INTO ClinicAlertSettings (TenantId, AlertType, DefaultUrgency)
VALUES (@t, 'DisconnectedMonitor', 'Red'), (@t, 'LowBattery', 'Yellow'), (@t, 'IrregularHeartbeat', 'Yellow');

INSERT INTO PatientScheduleSettings (TenantId, PatientId, IntervalDays) VALUES (@t, NULL, 30);
INSERT INTO ReportSettings (TenantId, PatientId, IntervalDays) VALUES (@t, NULL, 30);

INSERT INTO Patients (TenantId, MedicalRecordNumber, FirstName, LastName, DateOfBirth, PhoneNumber, Email, DeviceType, DeviceManufacturer, DeviceModel, DeviceSerialNumber, ImplantDate, BatteryLevel, LastHeartRate, LastSyncedAt, IsActive, CreatedAt)
VALUES {patients};");
        }
    }
}
