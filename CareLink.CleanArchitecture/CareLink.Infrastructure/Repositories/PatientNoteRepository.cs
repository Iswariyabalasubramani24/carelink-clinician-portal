using CareLink.Application.Common.Interfaces;
using CareLink.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CareLink.Infrastructure.Repositories;

public class PatientNoteRepository(ApplicationDbContext db) : IPatientNoteRepository
{
    public async Task<List<PatientNote>> GetByPatientIdAsync(int patientId)
    {
        return await db.PatientNotes
            .AsNoTracking()
            .Where(n => n.PatientId == patientId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task<PatientNote> AddAsync(PatientNote note)
    {
        db.PatientNotes.Add(note);
        await db.SaveChangesAsync();
        return note;
    }
}
