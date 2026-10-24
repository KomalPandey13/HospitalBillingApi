using HospitalBillingApi.Data;
using HospitalBillingApi.DTOs.Common;
using HospitalBillingApi.DTOs.Patients;
using HospitalBillingApi.Entities;
using HospitalBillingApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HospitalBillingApi.Controllers;

/// <summary>
/// Patient Management Endpoints: Registration, Pagination, Search, and Complete Billing History.
/// </summary>
[Route("api/[controller]")]
public class PatientsController : BaseApiController
{
    private readonly HospitalDbContext _context;
    private readonly IIdentifierGenerator _idGenerator;

    public PatientsController(HospitalDbContext context, IIdentifierGenerator idGenerator)
    {
        _context = context;
        _idGenerator = idGenerator;
    }

    /// <summary>
    /// Register a new patient. Auto-generates a unique UHID (Format: UHID-YYYYMMDD-XXXX).
    /// </summary>
    /// <param name="request">Patient details</param>
    /// <returns>Registered patient record with generated UHID</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<PatientResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RegisterPatient([FromBody] CreatePatientRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<object>.Fail("Validation failed.", errors));
        }

        var uhid = await _idGenerator.GenerateUhidAsync();

        var patient = new Patient
        {
            UHID = uhid,
            FullName = request.FullName.Trim(),
            Age = request.Age,
            Gender = request.Gender,
            ContactNumber = request.ContactNumber.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _context.Patients.Add(patient);
        await _context.SaveChangesAsync();

        var response = new PatientResponse
        {
            Id = patient.Id,
            UHID = patient.UHID,
            FullName = patient.FullName,
            Age = patient.Age,
            Gender = patient.Gender,
            ContactNumber = patient.ContactNumber,
            CreatedAt = patient.CreatedAt
        };

        return CreatedAtAction(nameof(GetPatientById), new { id = patient.Id }, ApiResponse<PatientResponse>.Ok(response, "Patient registered successfully."));
    }

    /// <summary>
    /// List patients with pagination and optional search filter by name or UHID.
    /// </summary>
    /// <param name="search">Optional search term for patient name or UHID</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 10, max: 100)</param>
    /// <returns>Paginated list of patients</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<PatientResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPatients([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var query = _context.Patients.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(p => p.FullName.ToLower().Contains(term) || p.UHID.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PatientResponse
            {
                Id = p.Id,
                UHID = p.UHID,
                FullName = p.FullName,
                Age = p.Age,
                Gender = p.Gender,
                ContactNumber = p.ContactNumber,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync();

        var pagedResult = new PagedResult<PatientResponse>(items, totalCount, page, pageSize);
        return Ok(ApiResponse<PagedResult<PatientResponse>>.Ok(pagedResult));
    }

    /// <summary>
    /// Get single patient by ID with full OPD and IPD billing history.
    /// </summary>
    /// <param name="id">Patient ID</param>
    /// <returns>Patient details with chronological OPD invoices and IPD admissions</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<PatientDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPatientById(int id)
    {
        var patient = await _context.Patients
            .AsNoTracking()
            .Include(p => p.OpdInvoices)
                .ThenInclude(o => o.Doctor)
            .Include(p => p.IpdAdmissions)
                .ThenInclude(a => a.AttendingDoctor)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (patient == null)
        {
            return NotFound(ApiResponse<object>.Fail($"Patient with ID {id} was not found."));
        }

        var detailResponse = new PatientDetailResponse
        {
            Id = patient.Id,
            UHID = patient.UHID,
            FullName = patient.FullName,
            Age = patient.Age,
            Gender = patient.Gender,
            ContactNumber = patient.ContactNumber,
            CreatedAt = patient.CreatedAt,
            OpdInvoices = patient.OpdInvoices
                .OrderByDescending(o => o.BillDate)
                .Select(o => new PatientOpdHistoryItem
                {
                    Id = o.Id,
                    InvoiceNumber = o.InvoiceNumber,
                    DoctorId = o.DoctorId,
                    DoctorName = o.Doctor.FullName,
                    Specialization = o.Doctor.Specialization,
                    BillDate = o.BillDate,
                    FeeAmount = o.FeeAmount,
                    PaymentMode = o.PaymentMode,
                    PaymentStatus = o.PaymentStatus,
                    CreatedAt = o.CreatedAt
                }).ToList(),
            IpdAdmissions = patient.IpdAdmissions
                .OrderByDescending(a => a.AdmissionDate)
                .Select(a => new PatientIpdHistoryItem
                {
                    Id = a.Id,
                    AdmissionNumber = a.AdmissionNumber,
                    AttendingDoctorId = a.AttendingDoctorId,
                    AttendingDoctorName = a.AttendingDoctor.FullName,
                    BedNumber = a.BedNumber,
                    AdmissionDate = a.AdmissionDate,
                    DischargeDate = a.DischargeDate,
                    DailyRoomRate = a.DailyRoomRate,
                    DaysStayed = a.CalculateDaysStayed(),
                    RoomCharges = a.CalculateRoomCharges(),
                    DoctorVisitCharges = a.DoctorVisitCharges,
                    PharmacyCharges = a.PharmacyCharges,
                    TotalBillAmount = a.TotalBillAmount,
                    Status = a.Status
                }).ToList()
        };

        return Ok(ApiResponse<PatientDetailResponse>.Ok(detailResponse));
    }
}
