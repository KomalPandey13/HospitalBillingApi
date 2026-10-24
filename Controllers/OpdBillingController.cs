using System.Globalization;
using HospitalBillingApi.Data;
using HospitalBillingApi.DTOs.Billing;
using HospitalBillingApi.DTOs.Common;
using HospitalBillingApi.Entities;
using HospitalBillingApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HospitalBillingApi.Controllers;

/// <summary>
/// OPD (Outpatient Department) Billing Management: Invoice Generation &amp; Daily Slips Retrieval.
/// </summary>
[Route("api/billing/opd")]
public class OpdBillingController : BaseApiController
{
    private readonly HospitalDbContext _context;
    private readonly IIdentifierGenerator _idGenerator;

    public OpdBillingController(HospitalDbContext context, IIdentifierGenerator idGenerator)
    {
        _context = context;
        _idGenerator = idGenerator;
    }

    /// <summary>
    /// Generate a new OPD invoice. Automatically snapshots the doctor's current consultation fee.
    /// </summary>
    /// <param name="request">OPD Billing details (PatientId, DoctorId, PaymentMode)</param>
    /// <returns>Generated OPD Invoice with unique invoice number</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<OpdInvoiceResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GenerateOpdInvoice([FromBody] CreateOpdInvoiceRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<object>.Fail("Validation failed.", errors));
        }

        var patient = await _context.Patients.FindAsync(request.PatientId);
        if (patient == null)
        {
            return NotFound(ApiResponse<object>.Fail($"Patient with ID {request.PatientId} was not found."));
        }

        var doctor = await _context.Doctors.FindAsync(request.DoctorId);
        if (doctor == null)
        {
            return NotFound(ApiResponse<object>.Fail($"Doctor with ID {request.DoctorId} was not found."));
        }

        if (!doctor.IsActive)
        {
            return BadRequest(ApiResponse<object>.Fail($"Doctor '{doctor.FullName}' is currently marked as inactive."));
        }

        var invoiceNumber = await _idGenerator.GenerateOpdInvoiceNumberAsync();
        var now = DateTime.UtcNow;

        var invoice = new OpdInvoice
        {
            InvoiceNumber = invoiceNumber,
            PatientId = patient.Id,
            DoctorId = doctor.Id,
            BillDate = now,
            FeeAmount = doctor.OpdCharge, // Snapshot doctor's consultation fee
            PaymentMode = request.PaymentMode,
            PaymentStatus = "Paid",
            CreatedAt = now
        };

        _context.OpdInvoices.Add(invoice);
        await _context.SaveChangesAsync();

        var response = new OpdInvoiceResponse
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            PatientId = patient.Id,
            PatientUhid = patient.UHID,
            PatientName = patient.FullName,
            DoctorId = doctor.Id,
            DoctorName = doctor.FullName,
            Specialization = doctor.Specialization,
            BillDate = invoice.BillDate,
            FeeAmount = invoice.FeeAmount,
            PaymentMode = invoice.PaymentMode,
            PaymentStatus = invoice.PaymentStatus,
            CreatedAt = invoice.CreatedAt
        };

        return CreatedAtAction(nameof(GetOpdInvoicesByDate), new { date = invoice.BillDate.ToString("yyyy-MM-dd") }, ApiResponse<OpdInvoiceResponse>.Ok(response, "OPD invoice generated successfully."));
    }

    /// <summary>
    /// List all OPD slips for a given date.
    /// </summary>
    /// <param name="date">Target date in 'yyyy-MM-dd' format (defaults to current UTC date if omitted)</param>
    /// <returns>List of OPD invoices for the specified date</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<OpdInvoiceResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetOpdInvoicesByDate([FromQuery] string? date)
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

        var invoices = await _context.OpdInvoices
            .AsNoTracking()
            .Include(o => o.Patient)
            .Include(o => o.Doctor)
            .Where(o => o.BillDate >= startOfDay && o.BillDate < endOfDay)
            .OrderByDescending(o => o.BillDate)
            .Select(o => new OpdInvoiceResponse
            {
                Id = o.Id,
                InvoiceNumber = o.InvoiceNumber,
                PatientId = o.PatientId,
                PatientUhid = o.Patient.UHID,
                PatientName = o.Patient.FullName,
                DoctorId = o.DoctorId,
                DoctorName = o.Doctor.FullName,
                Specialization = o.Doctor.Specialization,
                BillDate = o.BillDate,
                FeeAmount = o.FeeAmount,
                PaymentMode = o.PaymentMode,
                PaymentStatus = o.PaymentStatus,
                CreatedAt = o.CreatedAt
            })
            .ToListAsync();

        return Ok(ApiResponse<List<OpdInvoiceResponse>>.Ok(invoices, $"Found {invoices.Count} OPD invoices for {startOfDay:yyyy-MM-dd}."));
    }
}
