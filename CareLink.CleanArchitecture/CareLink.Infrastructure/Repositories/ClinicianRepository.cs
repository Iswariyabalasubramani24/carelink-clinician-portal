using CareLink.Application.Common.Interfaces;
using CareLink.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CareLink.Infrastructure.Repositories;

public class ClinicianRepository(ApplicationDbContext db) : IClinicianRepository
{
    public async Task<Clinician?> GetByEmailAsync(string email)
    {
        return await db.Clinicians.FirstOrDefaultAsync(c => c.Email == email);
    }
}
