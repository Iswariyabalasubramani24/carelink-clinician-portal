using CareLink.Application.Common.Interfaces;
using CareLink.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CareLink.Infrastructure.Seeding;

// Demo hospitals for local development and testing ONLY. Wired up in Program.cs
// behind an IsDevelopment() check so these well-known accounts never reach a
// real deployment. Idempotent: each hospital is keyed by region and skipped if
// it already exists.
public class DevelopmentDataSeeder(
    ApplicationDbContext db,
    IPasswordHasher passwordHasher,
    ILogger<DevelopmentDataSeeder> logger)
{
    private const string DoctorPassword = "Test@123";
    private const string AdminPassword = "Admin@123";
    private const int DefaultIntervalDays = 30;

    private static readonly (AlertType Type, AlertUrgency Urgency)[] DefaultAlertSettings =
    {
        (AlertType.DisconnectedMonitor, AlertUrgency.Red),
        (AlertType.LowBattery, AlertUrgency.Yellow),
        (AlertType.IrregularHeartbeat, AlertUrgency.Yellow)
    };

    private sealed record PatientSeed(
        string Mrn, string First, string Last, string Dob, string Phone, string Email,
        DeviceType Device, string Manufacturer, string Model, string Serial, string ImplantDate,
        decimal Battery, int HeartRate, int LastSyncedDaysAgo);

    public async Task SeedAsync()
    {
        await SeedHospitalAsync("Pompidou Hospital", "France", "fr",
            "doctor@pompidou.fr", "Camille", "Dubois", "admin@pompidou.fr", "Etienne", "Moreau",
            new PatientSeed("PMP-1001", "Emile", "Rousseau", "1968-06-11", "+33-1-23456789", "emile.rousseau@example.com", DeviceType.ICD, "Medtronic", "Evera XT", "MDT-ICD-FR001", "2022-09-10", 88.0m, 71, 2),
            new PatientSeed("PMP-1002", "Camille", "Petit", "1975-02-23", "+33-1-98765432", "camille.petit@example.com", DeviceType.Pacemaker, "Boston Scientific", "Accolade MRI", "BSX-PM-FR002", "2021-04-18", 91.0m, 69, 5));

        await SeedHospitalAsync("La Paz Hospital", "Spain", "es",
            "doctor@lapaz.es", "Lucia", "Fernandez", "admin@lapaz.es", "Mateo", "Garcia",
            new PatientSeed("LPZ-1001", "Lucia", "Martinez", "1970-11-05", "+34-91-1234567", "lucia.martinez@example.com", DeviceType.ICD, "Abbott", "Gallant HF", "ABT-ICD-ES001", "2023-01-22", 85.0m, 73, 1),
            new PatientSeed("LPZ-1002", "Javier", "Sanchez", "1982-08-30", "+34-91-7654321", "javier.sanchez@example.com", DeviceType.ICM, "Biotronik", "BioMonitor III", "BTK-ICM-ES002", "2023-07-14", 96.0m, 66, 3));

        await SeedHospitalAsync("Mercy General Hospital", "United States", "en",
            "doctor@mercygeneral.us", "Robert", "Johnson", "admin@mercygeneral.us", "Mary", "Williams",
            new PatientSeed("MGH-1001", "Robert", "Johnson", "1960-03-15", "+1-212-5550101", "robert.johnson@example.com", DeviceType.ICD, "Medtronic", "Cobalt XT", "MDT-ICD-US001", "2022-05-20", 84.0m, 75, 1),
            new PatientSeed("MGH-1002", "Mary", "Williams", "1978-07-08", "+1-212-5550102", "mary.williams@example.com", DeviceType.CRT_D, "Boston Scientific", "Resonate", "BSX-CRT-US002", "2021-12-03", 90.0m, 70, 4));

        await SeedHospitalAsync("St. Thomas Hospital", "United Kingdom", "en",
            "doctor@stthomas.uk", "William", "Brown", "admin@stthomas.uk", "Sophie", "Taylor",
            new PatientSeed("STH-1001", "William", "Brown", "1955-09-19", "+44-20-79460101", "william.brown@example.com", DeviceType.Pacemaker, "Abbott", "Assurity MRI", "ABT-PM-UK001", "2023-02-11", 93.0m, 67, 2),
            new PatientSeed("STH-1002", "Sophie", "Taylor", "1969-12-30", "+44-20-79460102", "sophie.taylor@example.com", DeviceType.ICM, "Medtronic", "Reveal LINQ", "MDT-ICM-UK002", "2023-06-25", 97.0m, 65, 6));

        await SeedHospitalAsync("Toronto General Hospital", "Canada", "en",
            "doctor@torontogeneral.ca", "David", "Wilson", "admin@torontogeneral.ca", "Emma", "Martin",
            new PatientSeed("TGH-1001", "David", "Wilson", "1962-04-27", "+1-416-5550101", "david.wilson@example.com", DeviceType.ICD, "Biotronik", "Rivacor 7", "BTK-ICD-CA001", "2022-08-14", 86.0m, 72, 1),
            new PatientSeed("TGH-1002", "Emma", "Martin", "1980-01-16", "+1-416-5550102", "emma.martin@example.com", DeviceType.Pacemaker, "Boston Scientific", "Accolade MRI", "BSX-PM-CA002", "2021-10-09", 89.0m, 68, 3));
    }

    private async Task SeedHospitalAsync(
        string name, string region, string lang,
        string docEmail, string docFirst, string docLast,
        string admEmail, string admFirst, string admLast,
        params PatientSeed[] patients)
    {
        if (await db.Tenants.AnyAsync(t => t.Region == region && !t.IsSystem))
        {
            return;
        }

        var tenant = new Tenant
        {
            Name = name,
            Region = region,
            LanguageCode = lang,
            IsActive = true,
            IsSystem = false,
            CreatedAt = DateTime.UtcNow
        };
        db.Tenants.Add(tenant);

        var doctor = NewClinician(tenant, docEmail, docFirst, docLast, ClinicianRole.Clinician, lang, DoctorPassword);
        var admin = NewClinician(tenant, admEmail, admFirst, admLast, ClinicianRole.Admin, lang, AdminPassword);
        db.Clinicians.Add(doctor);
        db.Clinicians.Add(admin);
        db.ClinicianTenants.Add(new ClinicianTenant { Clinician = doctor, Tenant = tenant });
        db.ClinicianTenants.Add(new ClinicianTenant { Clinician = admin, Tenant = tenant });

        foreach (var (type, urgency) in DefaultAlertSettings)
        {
            db.ClinicAlertSettings.Add(new ClinicAlertSettings { Tenant = tenant, AlertType = type, DefaultUrgency = urgency });
        }

        db.PatientScheduleSettings.Add(new PatientScheduleSettings { Tenant = tenant, IntervalDays = DefaultIntervalDays });
        db.ReportSettings.Add(new ReportSettings { Tenant = tenant, IntervalDays = DefaultIntervalDays });

        foreach (var p in patients)
        {
            db.Patients.Add(new Patient
            {
                Tenant = tenant,
                MedicalRecordNumber = p.Mrn,
                FirstName = p.First,
                LastName = p.Last,
                DateOfBirth = DateTime.Parse(p.Dob),
                PhoneNumber = p.Phone,
                Email = p.Email,
                DeviceType = p.Device,
                DeviceManufacturer = p.Manufacturer,
                DeviceModel = p.Model,
                DeviceSerialNumber = p.Serial,
                ImplantDate = DateTime.Parse(p.ImplantDate),
                BatteryLevel = p.Battery,
                LastHeartRate = p.HeartRate,
                LastSyncedAt = DateTime.UtcNow.AddDays(-p.LastSyncedDaysAgo),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync(CancellationToken.None);
        logger.LogInformation("Seeded development hospital {Name} ({Region}).", name, region);
    }

    private Clinician NewClinician(Tenant tenant, string email, string first, string last, ClinicianRole role, string lang, string password) => new()
    {
        Tenant = tenant,
        Email = email,
        PasswordHash = passwordHasher.Hash(password),
        FirstName = first,
        LastName = last,
        Role = role,
        LanguageCode = lang,
        IsActive = true,
        CreatedAt = DateTime.UtcNow
    };
}
