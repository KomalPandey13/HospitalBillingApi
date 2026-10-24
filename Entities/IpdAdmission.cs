using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HospitalBillingApi.Entities;

public class IpdAdmission
{
    public int Id { get; set; }

    [Required]
    [MaxLength(30)]
    public string AdmissionNumber { get; set; } = string.Empty;

    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public int AttendingDoctorId { get; set; }
    public Doctor AttendingDoctor { get; set; } = null!;

    [Required]
    [MaxLength(30)]
    public string BedNumber { get; set; } = string.Empty;

    public DateTime AdmissionDate { get; set; }

    public DateTime? DischargeDate { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Range(0, 1000000)]
    public decimal DailyRoomRate { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Range(0, 10000000)]
    public decimal DoctorVisitCharges { get; set; } = 0m;

    [Column(TypeName = "decimal(18,2)")]
    [Range(0, 10000000)]
    public decimal PharmacyCharges { get; set; } = 0m;

    [Column(TypeName = "decimal(18,2)")]
    [Range(0, 100000000)]
    public decimal TotalBillAmount { get; set; } = 0m;

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Admitted"; // Admitted, Discharged, Settled

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Calculates the number of days stayed.
    /// If discharged on the same day or duration &lt; 1 day, standard hospital billing counts it as at least 1 day.
    /// </summary>
    public int CalculateDaysStayed(DateTime? effectiveDischarge = null)
    {
        var discharge = effectiveDischarge ?? DischargeDate ?? DateTime.UtcNow;
        if (discharge < AdmissionDate)
            return 1;

        var days = (int)Math.Ceiling((discharge - AdmissionDate).TotalDays);
        return days <= 0 ? 1 : days;
    }

    /// <summary>
    /// Calculates the room charges based on daily room rate and days stayed.
    /// </summary>
    public decimal CalculateRoomCharges(DateTime? effectiveDischarge = null)
    {
        return CalculateDaysStayed(effectiveDischarge) * DailyRoomRate;
    }
}
