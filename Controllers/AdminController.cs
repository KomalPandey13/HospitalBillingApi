using System.Globalization;
using HospitalBillingApi.Data;
using HospitalBillingApi.DTOs.Admin;
using HospitalBillingApi.DTOs.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HospitalBillingApi.Controllers;

/// <summary>
/// Admin Dashboard &amp; Settlement Engine: Hospital revenue metrics and doctor daily payout reconciliations.
/// </summary>
[Route("api/admin")]
public class AdminController : BaseApiController
{
    private readonly HospitalDbContext _context;

    public AdminController(HospitalDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Get hospital-wide revenue, patient registration, and consultation metrics for a specific date.
    /// </summary>
    /// <param name="date">Target date in 'yyyy-MM-dd' format (defaults to current UTC date if omitted)</param>
    /// <returns>Admin metrics summarizing lifetime patients, daily OPD revenue, daily IPD settled revenue, and grand total</returns>
    [HttpGet("metrics")]
    [ProducesResponseType(typeof(ApiResponse<AdminMetricsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAdminMetrics([FromQuery] string? date)
    {
        DateTime targetDate;
        if (string.IsNullOrWhiteSpace(date))
        {
            targetDate = DateTime.UtcNow.Date;
        }
        else
        {
            if (!DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out targetDate))
            {
                return BadRequest(ApiResponse<object>.Fail("Invalid date format. Please provide date in 'yyyy-MM-dd' format."));
            }
        }

        var startOfDay = targetDate.Date;
        var endOfDay = startOfDay.AddDays(1);

        // 1. Lifetime registered patient count
        var totalRegisteredPatients = await _context.Patients.CountAsync();

        // 2. OPD metrics for the target date
        var opdQuery = _context.OpdInvoices
            .Where(o => o.BillDate >= startOfDay && o.BillDate < endOfDay && o.PaymentStatus == "Paid");

        var totalOpdFees = await opdQuery.SumAsync(o => (decimal?)o.FeeAmount) ?? 0m;
        var totalConsultationCount = await opdQuery.CountAsync();

        // 3. IPD settled metrics for the target date
        var ipdQuery = _context.IpdAdmissions
            .Where(a => a.DischargeDate.HasValue &&
                        a.DischargeDate.Value >= startOfDay &&
                        a.DischargeDate.Value < endOfDay &&
                        (a.Status == "Settled" || a.Status == "Discharged"));

        var totalIpdFees = await ipdQuery.SumAsync(a => (decimal?)a.TotalBillAmount) ?? 0m;
        var totalIpdSettledCount = await ipdQuery.CountAsync();

        // 4. Grand total
        var grandTotalRevenue = totalOpdFees + totalIpdFees;

        var metrics = new AdminMetricsResponse
        {
            Date = startOfDay.ToString("yyyy-MM-dd"),
            TotalRegisteredPatients = totalRegisteredPatients,
            TotalOpdFees = Math.Round(totalOpdFees, 2),
            TotalIpdFees = Math.Round(totalIpdFees, 2),
            GrandTotalRevenue = Math.Round(grandTotalRevenue, 2),
            TotalConsultationCount = totalConsultationCount,
            TotalIpdSettledCount = totalIpdSettledCount
        };

        return Ok(ApiResponse<AdminMetricsResponse>.Ok(metrics, $"Metrics retrieved for {startOfDay:yyyy-MM-dd}."));
    }

    /// <summary>
    /// Calculate daily doctor consultation counts, gross fee collections, and net payout settlements.
    /// </summary>
    /// <param name="date">Target settlement date in 'yyyy-MM-dd' format (defaults to current UTC date if omitted)</param>
    /// <returns>Array of active doctors with consultation count, gross collection, net payout, and hospital retainage</returns>
    [HttpGet("doctors/daily-fees")]
    [ProducesResponseType(typeof(ApiResponse<List<DoctorDailyFeeResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetDoctorDailyFees([FromQuery] string? date)
    {
        DateTime targetDate;
        if (string.IsNullOrWhiteSpace(date))
        {
            targetDate = DateTime.UtcNow.Date;
        }
        else
        {
            if (!DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out targetDate))
            {
                return BadRequest(ApiResponse<object>.Fail("Invalid date format. Please provide date in 'yyyy-MM-dd' format."));
            }
        }

        var startOfDay = targetDate.Date;
        var endOfDay = startOfDay.AddDays(1);

        // Fetch all active doctors
        var activeDoctors = await _context.Doctors
            .AsNoTracking()
            .Where(d => d.IsActive)
            .OrderBy(d => d.FullName)
            .ToListAsync();

        // Fetch all OPD invoices for that date grouped by doctor
        var doctorInvoices = await _context.OpdInvoices
            .AsNoTracking()
            .Where(o => o.BillDate >= startOfDay && o.BillDate < endOfDay && o.PaymentStatus == "Paid")
            .ToListAsync();

        var opdByDoctor = doctorInvoices
            .GroupBy(o => o.DoctorId)
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    Count = g.Count(),
                    Gross = g.Sum(x => x.FeeAmount)
                }
            );

        var settlements = new List<DoctorDailyFeeResponse>();

        foreach (var doctor in activeDoctors)
        {
            var count = 0;
            var gross = 0m;

            if (opdByDoctor.TryGetValue(doctor.Id, out var stat))
            {
                count = stat.Count;
                gross = stat.Gross;
            }

            var netPayout = Math.Round(gross * (doctor.PayoutPercentage / 100m), 2);
            var hospitalShare = Math.Round(gross - netPayout, 2);

            settlements.Add(new DoctorDailyFeeResponse
            {
                DoctorId = doctor.Id,
                DoctorName = doctor.FullName,
                Specialization = doctor.Specialization,
                OpdCharge = doctor.OpdCharge,
                PayoutPercentage = doctor.PayoutPercentage,
                ConsultationCount = count,
                GrossFeeCollected = Math.Round(gross, 2),
                DoctorNetPayout = netPayout,
                HospitalShare = hospitalShare
            });
        }

        return Ok(ApiResponse<List<DoctorDailyFeeResponse>>.Ok(settlements, $"Settlement calculation completed for {startOfDay:yyyy-MM-dd}. Active doctors: {settlements.Count}."));
    }
}
