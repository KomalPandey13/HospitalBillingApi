using HospitalBillingApi.Data;
using Microsoft.EntityFrameworkCore;

namespace HospitalBillingApi.Services;

public class IdentifierGenerator : IIdentifierGenerator
{
    private readonly HospitalDbContext _context;
    private static readonly SemaphoreSlim _uhidLock = new(1, 1);
    private static readonly SemaphoreSlim _opdLock = new(1, 1);
    private static readonly SemaphoreSlim _ipdLock = new(1, 1);

    public IdentifierGenerator(HospitalDbContext context)
    {
        _context = context;
    }

    public async Task<string> GenerateUhidAsync(DateTime? date = null)
    {
        var targetDate = date ?? DateTime.UtcNow;
        var datePart = targetDate.ToString("yyyyMMdd");
        var prefix = $"UHID-{datePart}-";

        await _uhidLock.WaitAsync();
        try
        {
            var existingUhids = await _context.Patients
                .Where(p => p.UHID.StartsWith(prefix))
                .Select(p => p.UHID)
                .ToListAsync();

            var maxSeq = 0;
            foreach (var uhid in existingUhids)
            {
                var suffix = uhid.Substring(prefix.Length);
                if (int.TryParse(suffix, out var seq) && seq > maxSeq)
                {
                    maxSeq = seq;
                }
            }

            var nextSeq = maxSeq + 1;
            return $"{prefix}{nextSeq:D4}";
        }
        finally
        {
            _uhidLock.Release();
        }
    }

    public async Task<string> GenerateOpdInvoiceNumberAsync(DateTime? date = null)
    {
        var targetDate = date ?? DateTime.UtcNow;
        var datePart = targetDate.ToString("yyyyMMdd");
        var prefix = $"OPD-{datePart}-";

        await _opdLock.WaitAsync();
        try
        {
            var existingInvoices = await _context.OpdInvoices
                .Where(o => o.InvoiceNumber.StartsWith(prefix))
                .Select(o => o.InvoiceNumber)
                .ToListAsync();

            var maxSeq = 0;
            foreach (var inv in existingInvoices)
            {
                var suffix = inv.Substring(prefix.Length);
                if (int.TryParse(suffix, out var seq) && seq > maxSeq)
                {
                    maxSeq = seq;
                }
            }

            var nextSeq = maxSeq + 1;
            return $"{prefix}{nextSeq:D4}";
        }
        finally
        {
            _opdLock.Release();
        }
    }

    public async Task<string> GenerateIpdAdmissionNumberAsync(DateTime? date = null)
    {
        var targetDate = date ?? DateTime.UtcNow;
        var datePart = targetDate.ToString("yyyyMMdd");
        var prefix = $"IPD-{datePart}-";

        await _ipdLock.WaitAsync();
        try
        {
            var existingAdmissions = await _context.IpdAdmissions
                .Where(a => a.AdmissionNumber.StartsWith(prefix))
                .Select(a => a.AdmissionNumber)
                .ToListAsync();

            var maxSeq = 0;
            foreach (var adm in existingAdmissions)
            {
                var suffix = adm.Substring(prefix.Length);
                if (int.TryParse(suffix, out var seq) && seq > maxSeq)
                {
                    maxSeq = seq;
                }
            }

            var nextSeq = maxSeq + 1;
            return $"{prefix}{nextSeq:D4}";
        }
        finally
        {
            _ipdLock.Release();
        }
    }
}
