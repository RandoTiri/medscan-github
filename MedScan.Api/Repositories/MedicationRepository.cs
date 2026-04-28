using MedScan.Api.Data;
using MedScan.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace MedScan.Api.Repositories;

public sealed class MedicationRepository : IMedicationRepository {
    private readonly AppDbContext _db;

    public MedicationRepository(AppDbContext db) {
        _db = db;
    }

    public async Task<Medication?> FindByBarcodeAsync(string barcode)
    {
        var variants = BuildBarcodeVariants(barcode);
        if (variants.Count == 0)
        {
            return null;
        }

        var matches = await _db.Medications
            .AsNoTracking()
            .Where(m =>
                variants.Contains(m.Barcode) ||
                variants.Contains(m.Barcode.Replace(" ", string.Empty).Replace("-", string.Empty)))
            .ToListAsync();

        if (matches.Count == 0)
        {
            return null;
        }

        // Prefer exact incoming format first, then any compatible variant.
        return matches
            .OrderByDescending(m => string.Equals(m.Barcode, barcode, StringComparison.Ordinal))
            .ThenByDescending(m => m.Barcode.Length)
            .FirstOrDefault();
    }

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

    private static HashSet<string> BuildBarcodeVariants(string barcode)
    {
        var variants = new HashSet<string>(StringComparer.Ordinal);
        if (string.IsNullOrWhiteSpace(barcode))
        {
            return variants;
        }

        var normalized = new string(barcode.Trim().Where(char.IsDigit).ToArray());
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return variants;
        }

        variants.Add(normalized);

        if (normalized.Length == 14 && normalized.StartsWith('0'))
        {
            variants.Add(normalized[1..]); // GTIN-14 -> EAN-13
        }
        else if (normalized.Length == 13)
        {
            variants.Add($"0{normalized}"); // EAN-13 -> GTIN-14
        }

        return variants;
    }
}
