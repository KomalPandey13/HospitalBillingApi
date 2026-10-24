using System.ComponentModel.DataAnnotations;

namespace HospitalBillingApi.DTOs.Billing;

public class AdmitPatientRequest
{
    [Required(ErrorMessage = "PatientId is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "PatientId must be a valid positive integer.")]
    public int PatientId { get; set; }

    [Required(ErrorMessage = "AttendingDoctorId is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "AttendingDoctorId must be a valid positive integer.")]
    public int AttendingDoctorId { get; set; }

    [Required(ErrorMessage = "BedNumber is required.")]
    [StringLength(30, MinimumLength = 1, ErrorMessage = "BedNumber must be between 1 and 30 characters.")]
    public string BedNumber { get; set; } = string.Empty;

    [Range(0, 1000000, ErrorMessage = "DailyRoomRate must be between 0 and 1,000,000.")]
    public decimal DailyRoomRate { get; set; }

    public DateTime? AdmissionDate { get; set; }
}

public class DischargePatientRequest
{
    public DateTime? DischargeDate { get; set; }

    [Range(0, 10000000, ErrorMessage = "DoctorVisitCharges must be non-negative.")]
    public decimal DoctorVisitCharges { get; set; } = 0m;

    [Range(0, 10000000, ErrorMessage = "PharmacyCharges must be non-negative.")]
    public decimal PharmacyCharges { get; set; } = 0m;
}

public class IpdAdmissionResponse
{
    public int Id { get; set; }
    public string AdmissionNumber { get; set; } = string.Empty;
    public int PatientId { get; set; }
    public string PatientUhid { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public int AttendingDoctorId { get; set; }
    public string AttendingDoctorName { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public string BedNumber { get; set; } = string.Empty;
    public DateTime AdmissionDate { get; set; }
    public DateTime? DischargeDate { get; set; }
    public decimal DailyRoomRate { get; set; }
    public int DaysStayed { get; set; }
    public decimal CurrentRoomCharges { get; set; }
    public decimal DoctorVisitCharges { get; set; }
    public decimal PharmacyCharges { get; set; }
    public decimal TotalBillAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class IpdDischargeResponse
{
    public int Id { get; set; }
    public string AdmissionNumber { get; set; } = string.Empty;
    public int PatientId { get; set; }
    public string PatientUhid { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public int AttendingDoctorId { get; set; }
    public string AttendingDoctorName { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public string BedNumber { get; set; } = string.Empty;
    public DateTime AdmissionDate { get; set; }
    public DateTime DischargeDate { get; set; }
    public int DaysStayed { get; set; }
    public decimal DailyRoomRate { get; set; }
    public decimal RoomCharges { get; set; }
    public decimal DoctorVisitCharges { get; set; }
    public decimal PharmacyCharges { get; set; }
    public decimal TotalBillAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string SummaryMessage { get; set; } = string.Empty;
}
