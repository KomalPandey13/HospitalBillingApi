using System.ComponentModel.DataAnnotations;

namespace HospitalBillingApi.DTOs.Doctors;

public class CreateDoctorRequest
{
    [Required(ErrorMessage = "Doctor full name is required.")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "Doctor name must be between 2 and 150 characters.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Specialization is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Specialization must be between 2 and 100 characters.")]
    public string Specialization { get; set; } = string.Empty;

    [Range(0, 1000000, ErrorMessage = "OPD Charge must be between 0 and 1,000,000.")]
    public decimal OpdCharge { get; set; }

    [Range(0, 100, ErrorMessage = "Payout Percentage must be between 0 and 100.")]
    public decimal PayoutPercentage { get; set; } = 70.0m;

    public bool IsActive { get; set; } = true;
}

public class DoctorResponse
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public decimal OpdCharge { get; set; }
    public decimal PayoutPercentage { get; set; }
    public decimal HospitalRetentionPercentage => 100m - PayoutPercentage;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
