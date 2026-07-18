using CareLink.Domain.Entities;

namespace CareLink.Application.Common.Interfaces;

public interface IClinicianRepository
{
    Task<Clinician?> GetByEmailAsync(string email);
}
