using HospitalBillingApi.Data;
using HospitalBillingApi.DTOs.Common;
using HospitalBillingApi.DTOs.Doctors;
using HospitalBillingApi.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HospitalBillingApi.Controllers;

/// <summary>
/// Doctor Management Endpoints: Add doctors and retrieve active doctors.
/// </summary>
[Route("api/[controller]")]
public class DoctorsController : BaseApiController
{
    private readonly HospitalDbContext _context;

    public DoctorsController(HospitalDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Add a new doctor with specialization, OPD consultation fee, and revenue share payout percentage.
    /// </summary>
    /// <param name="request">Doctor profile and billing settings</param>
    /// <returns>Created doctor record</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<DoctorResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddDoctor([FromBody] CreateDoctorRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<object>.Fail("Validation failed.", errors));
        }

        var doctor = new Doctor
        {
            FullName = request.FullName.Trim(),
            Specialization = request.Specialization.Trim(),
            OpdCharge = request.OpdCharge,
            PayoutPercentage = request.PayoutPercentage,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _context.Doctors.Add(doctor);
        await _context.SaveChangesAsync();

        var response = new DoctorResponse
        {
            Id = doctor.Id,
            FullName = doctor.FullName,
            Specialization = doctor.Specialization,
            OpdCharge = doctor.OpdCharge,
            PayoutPercentage = doctor.PayoutPercentage,
            IsActive = doctor.IsActive,
            CreatedAt = doctor.CreatedAt
        };

        return CreatedAtAction(nameof(GetDoctors), new { id = doctor.Id }, ApiResponse<DoctorResponse>.Ok(response, "Doctor added successfully."));
    }

    /// <summary>
    /// List doctors (defaults to active doctors).
    /// </summary>
    /// <param name="onlyActive">Filter to only active doctors (default: true)</param>
    /// <returns>List of doctor profiles with OPD charges and payout percentage</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<DoctorResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDoctors([FromQuery] bool onlyActive = true)
    {
        var query = _context.Doctors.AsNoTracking().AsQueryable();

        if (onlyActive)
        {
            query = query.Where(d => d.IsActive);
        }

        var doctors = await query
            .OrderBy(d => d.FullName)
            .Select(d => new DoctorResponse
            {
                Id = d.Id,
                FullName = d.FullName,
                Specialization = d.Specialization,
                OpdCharge = d.OpdCharge,
                PayoutPercentage = d.PayoutPercentage,
                IsActive = d.IsActive,
                CreatedAt = d.CreatedAt
            })
            .ToListAsync();

        return Ok(ApiResponse<List<DoctorResponse>>.Ok(doctors));
    }
}
