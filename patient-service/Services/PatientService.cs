using CareLink.PatientService.Data;
using CareLink.PatientService.DTOs;
using CareLink.PatientService.Models;
using Microsoft.EntityFrameworkCore;

namespace CareLink.PatientService.Services;

public class PatientService(PatientDbContext context) : IPatientService
{
    public async Task<IEnumerable<PatientDto>> GetAllAsync()
    {
        return await context.Patients
            .AsNoTracking()
            .OrderBy(p => p.LastName)
            .Select(p => ToDto(p))
            .ToListAsync();
    }

    public async Task<PatientDto?> GetByIdAsync(int id)
    {
        var patient = await context.Patients.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        return patient is null ? null : ToDto(patient);
    }

    public async Task<PatientDto> CreateAsync(CreatePatientDto dto)
    {
        var patient = new Patient
        {
            MedicalRecordNumber = dto.MedicalRecordNumber,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            DateOfBirth = dto.DateOfBirth,
            PhoneNumber = dto.PhoneNumber,
            Email = dto.Email,
            DeviceType = dto.DeviceType,
            DeviceManufacturer = dto.DeviceManufacturer,
            DeviceModel = dto.DeviceModel,
            DeviceSerialNumber = dto.DeviceSerialNumber,
            ImplantDate = dto.ImplantDate,
            CreatedAt = DateTime.UtcNow
        };

        context.Patients.Add(patient);
        await context.SaveChangesAsync();

        return ToDto(patient);
    }

    public async Task<PatientDto?> UpdateAsync(int id, UpdatePatientDto dto)
    {
        var patient = await context.Patients.FirstOrDefaultAsync(p => p.Id == id);
        if (patient is null)
        {
            return null;
        }

        patient.FirstName = dto.FirstName;
        patient.LastName = dto.LastName;
        patient.PhoneNumber = dto.PhoneNumber;
        patient.Email = dto.Email;
        patient.DeviceType = dto.DeviceType;
        patient.DeviceManufacturer = dto.DeviceManufacturer;
        patient.DeviceModel = dto.DeviceModel;
        patient.ImplantDate = dto.ImplantDate;
        patient.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return ToDto(patient);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var patient = await context.Patients.FirstOrDefaultAsync(p => p.Id == id);
        if (patient is null)
        {
            return false;
        }

        context.Patients.Remove(patient);
        await context.SaveChangesAsync();
        return true;
    }

    private static PatientDto ToDto(Patient p) => new(
        p.Id,
        p.MedicalRecordNumber,
        p.FirstName,
        p.LastName,
        p.DateOfBirth,
        p.PhoneNumber,
        p.Email,
        p.DeviceType,
        p.DeviceManufacturer,
        p.DeviceModel,
        p.DeviceSerialNumber,
        p.ImplantDate,
        p.CreatedAt,
        p.UpdatedAt);
}
