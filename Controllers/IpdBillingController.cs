using HospitalBillingApi.Data;
using HospitalBillingApi.DTOs.Billing;
using HospitalBillingApi.DTOs.Common;
using HospitalBillingApi.Entities;
using HospitalBillingApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HospitalBillingApi.Controllers;

/// <summary>
/// IPD (Inpatient Department) Billing &amp; Discharge Management: Patient Admission, Active Bed Tracking &amp; Discharge Settlement.
/// </summary>
[Route("api/billing/ipd")]
public class IpdBillingController : BaseApiController
{
    private readonly HospitalDbContext _context;
    private readonly IIdentifierGenerator _idGenerator;

    public IpdBillingController(HospitalDbContext context, IIdentifierGenerator idGenerator)
    {
        _context = context;
        _idGenerator = idGenerator;
    }

    /// <summary>
    /// Admit a patient to an IPD bed.
    /// </summary>
    /// <param name="request">Admission details (PatientId, AttendingDoctorId, BedNumber, DailyRoomRate)</param>
    /// <returns>Created admission record with unique admission number</returns>
    [HttpPost("admit")]
    [ProducesResponseType(typeof(ApiResponse<IpdAdmissionResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AdmitPatient([FromBody] AdmitPatientRequest request)
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

        var doctor = await _context.Doctors.FindAsync(request.AttendingDoctorId);
        if (doctor == null)
        {
            return NotFound(ApiResponse<object>.Fail($"Doctor with ID {request.AttendingDoctorId} was not found."));
        }

        if (!doctor.IsActive)
        {
            return BadRequest(ApiResponse<object>.Fail($"Doctor '{doctor.FullName}' is currently marked as inactive."));
        }

        // Check if patient is already admitted
        var existingPatientAdmission = await _context.IpdAdmissions
            .FirstOrDefaultAsync(a => a.PatientId == request.PatientId && a.Status == "Admitted");
        if (existingPatientAdmission != null)
        {
            return Conflict(ApiResponse<object>.Fail($"Patient '{patient.FullName}' is already admitted in Bed '{existingPatientAdmission.BedNumber}' (Admission #: {existingPatientAdmission.AdmissionNumber})."));
        }

        // Check if bed is already occupied
        var bedOccupied = await _context.IpdAdmissions
            .FirstOrDefaultAsync(a => a.BedNumber.ToLower() == request.BedNumber.Trim().ToLower() && a.Status == "Admitted");
        if (bedOccupied != null)
        {
            return Conflict(ApiResponse<object>.Fail($"Bed '{request.BedNumber}' is currently occupied by admission '{bedOccupied.AdmissionNumber}'."));
        }

        var admissionDate = request.AdmissionDate ?? DateTime.UtcNow;
        var admissionNumber = await _idGenerator.GenerateIpdAdmissionNumberAsync(admissionDate);

        var admission = new IpdAdmission
        {
            AdmissionNumber = admissionNumber,
            PatientId = patient.Id,
            AttendingDoctorId = doctor.Id,
            BedNumber = request.BedNumber.Trim(),
            AdmissionDate = admissionDate,
            DailyRoomRate = request.DailyRoomRate,
            DoctorVisitCharges = 0m,
            PharmacyCharges = 0m,
            TotalBillAmount = 0m,
            Status = "Admitted",
            CreatedAt = DateTime.UtcNow
        };

        _context.IpdAdmissions.Add(admission);
        await _context.SaveChangesAsync();

        var daysStayed = admission.CalculateDaysStayed();
        var roomCharges = admission.CalculateRoomCharges();

        var response = new IpdAdmissionResponse
        {
            Id = admission.Id,
            AdmissionNumber = admission.AdmissionNumber,
            PatientId = patient.Id,
            PatientUhid = patient.UHID,
            PatientName = patient.FullName,
            AttendingDoctorId = doctor.Id,
            AttendingDoctorName = doctor.FullName,
            Specialization = doctor.Specialization,
            BedNumber = admission.BedNumber,
            AdmissionDate = admission.AdmissionDate,
            DischargeDate = null,
            DailyRoomRate = admission.DailyRoomRate,
            DaysStayed = daysStayed,
            CurrentRoomCharges = roomCharges,
            DoctorVisitCharges = admission.DoctorVisitCharges,
            PharmacyCharges = admission.PharmacyCharges,
            TotalBillAmount = roomCharges,
            Status = admission.Status,
            CreatedAt = admission.CreatedAt
        };

        return CreatedAtAction(nameof(GetActiveAdmissions), new { id = admission.Id }, ApiResponse<IpdAdmissionResponse>.Ok(response, "Patient admitted successfully."));
    }

    /// <summary>
    /// Settle and discharge an admitted patient. Calculates room rate * days stayed + doctor visits + pharmacy charges.
    /// </summary>
    /// <param name="id">Admission ID</param>
    /// <param name="request">Discharge settlement details (DischargeDate, DoctorVisitCharges, PharmacyCharges)</param>
    /// <returns>Final settled IPD invoice with detailed breakdown</returns>
    [HttpPost("discharge/{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<IpdDischargeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DischargePatient(int id, [FromBody] DischargePatientRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<object>.Fail("Validation failed.", errors));
        }

        var admission = await _context.IpdAdmissions
            .Include(a => a.Patient)
            .Include(a => a.AttendingDoctor)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (admission == null)
        {
            return NotFound(ApiResponse<object>.Fail($"IPD Admission with ID {id} was not found."));
        }

        if (admission.Status != "Admitted")
        {
            return BadRequest(ApiResponse<object>.Fail($"Admission '{admission.AdmissionNumber}' cannot be settled. Current status: '{admission.Status}'."));
        }

        var dischargeDate = request.DischargeDate ?? DateTime.UtcNow;
        if (dischargeDate < admission.AdmissionDate)
        {
            return BadRequest(ApiResponse<object>.Fail($"Discharge date ({dischargeDate:yyyy-MM-dd HH:mm}) cannot be earlier than admission date ({admission.AdmissionDate:yyyy-MM-dd HH:mm})."));
        }

        // Calculate days stayed (minimum 1 day for same-day discharge)
        var daysStayed = admission.CalculateDaysStayed(dischargeDate);
        var roomCharges = daysStayed * admission.DailyRoomRate;
        var totalBill = roomCharges + request.DoctorVisitCharges + request.PharmacyCharges;

        admission.DischargeDate = dischargeDate;
        admission.DoctorVisitCharges = request.DoctorVisitCharges;
        admission.PharmacyCharges = request.PharmacyCharges;
        admission.TotalBillAmount = totalBill;
        admission.Status = "Settled";

        await _context.SaveChangesAsync();

        var response = new IpdDischargeResponse
        {
            Id = admission.Id,
            AdmissionNumber = admission.AdmissionNumber,
            PatientId = admission.PatientId,
            PatientUhid = admission.Patient.UHID,
            PatientName = admission.Patient.FullName,
            AttendingDoctorId = admission.AttendingDoctorId,
            AttendingDoctorName = admission.AttendingDoctor.FullName,
            Specialization = admission.AttendingDoctor.Specialization,
            BedNumber = admission.BedNumber,
            AdmissionDate = admission.AdmissionDate,
            DischargeDate = dischargeDate,
            DaysStayed = daysStayed,
            DailyRoomRate = admission.DailyRoomRate,
            RoomCharges = roomCharges,
            DoctorVisitCharges = request.DoctorVisitCharges,
            PharmacyCharges = request.PharmacyCharges,
            TotalBillAmount = totalBill,
            Status = admission.Status,
            SummaryMessage = $"Patient discharged from Bed {admission.BedNumber}. Stayed {daysStayed} day(s). Total settled bill: {totalBill:C}."
        };

        return Ok(ApiResponse<IpdDischargeResponse>.Ok(response, "IPD patient discharged and bill settled successfully."));
    }

    /// <summary>
    /// List all currently admitted patients with live stay and running charge estimations.
    /// </summary>
    /// <returns>List of currently occupied beds and active admissions</returns>
    [HttpGet("active")]
    [ProducesResponseType(typeof(ApiResponse<List<IpdAdmissionResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActiveAdmissions()
    {
        var activeAdmissions = await _context.IpdAdmissions
            .AsNoTracking()
            .Include(a => a.Patient)
            .Include(a => a.AttendingDoctor)
            .Where(a => a.Status == "Admitted")
            .OrderBy(a => a.AdmissionDate)
            .ToListAsync();

        var responseList = activeAdmissions.Select(a =>
        {
            var days = a.CalculateDaysStayed();
            var roomCharges = a.CalculateRoomCharges();
            return new IpdAdmissionResponse
            {
                Id = a.Id,
                AdmissionNumber = a.AdmissionNumber,
                PatientId = a.PatientId,
                PatientUhid = a.Patient.UHID,
                PatientName = a.Patient.FullName,
                AttendingDoctorId = a.AttendingDoctorId,
                AttendingDoctorName = a.AttendingDoctor.FullName,
                Specialization = a.AttendingDoctor.Specialization,
                BedNumber = a.BedNumber,
                AdmissionDate = a.AdmissionDate,
                DischargeDate = null,
                DailyRoomRate = a.DailyRoomRate,
                DaysStayed = days,
                CurrentRoomCharges = roomCharges,
                DoctorVisitCharges = a.DoctorVisitCharges,
                PharmacyCharges = a.PharmacyCharges,
                TotalBillAmount = roomCharges + a.DoctorVisitCharges + a.PharmacyCharges,
                Status = a.Status,
                CreatedAt = a.CreatedAt
            };
        }).ToList();

        return Ok(ApiResponse<List<IpdAdmissionResponse>>.Ok(responseList, $"Found {responseList.Count} active IPD admissions."));
    }
}
