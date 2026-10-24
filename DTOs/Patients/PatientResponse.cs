namespace HospitalBillingApi.DTOs.Patients;

public class PatientResponse
{
    public int Id { get; set; }
    public string UHID { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string ContactNumber { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class PatientDetailResponse : PatientResponse
{
    public List<PatientOpdHistoryItem> OpdInvoices { get; set; } = new();
    public List<PatientIpdHistoryItem> IpdAdmissions { get; set; } = new();
}

public class PatientOpdHistoryItem
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public int DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public DateTime BillDate { get; set; }
    public decimal FeeAmount { get; set; }
    public string PaymentMode { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class PatientIpdHistoryItem
{
    public int Id { get; set; }
    public string AdmissionNumber { get; set; } = string.Empty;
    public int AttendingDoctorId { get; set; }
    public string AttendingDoctorName { get; set; } = string.Empty;
    public string BedNumber { get; set; } = string.Empty;
    public DateTime AdmissionDate { get; set; }
    public DateTime? DischargeDate { get; set; }
    public decimal DailyRoomRate { get; set; }
    public int DaysStayed { get; set; }
    public decimal RoomCharges { get; set; }
    public decimal DoctorVisitCharges { get; set; }
    public decimal PharmacyCharges { get; set; }
    public decimal TotalBillAmount { get; set; }
    public string Status { get; set; } = string.Empty;
}
