using MedScan.Api.Data;
using MedScan.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace MedScan.Api.Repositories;

public sealed class MedicationRepository : IMedicationRepository {
    private readonly AppDbContext _db;

    public MedicationRepository(AppDbContext db) {
        _db = db;
    }

    public Task<Medication?> FindByBarcodeAsync(string barcode)
        => _db.Medications
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Barcode == barcode);

    public Task<Medication?> FindByIdAsync(int id)
        => _db.Medications
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id);

    public async Task<IEnumerable<Medication>> SearchByNameAsync(string name) {
        var pattern = $"%{name.Trim()}%";
        return await _db.Medications
            .Where(m => EF.Functions.ILike(m.Name,pattern))
            .OrderBy(m => m.Name)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task AddAsync(Medication medication) {
        await _db.Medications.AddAsync(medication);
    }

    public Task SaveChangesAsync() {
        return _db.SaveChangesAsync();
    }
}
