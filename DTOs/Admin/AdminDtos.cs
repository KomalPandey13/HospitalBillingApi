namespace HospitalBillingApi.DTOs.Admin;

public class AdminMetricsResponse
{
    public string Date { get; set; } = string.Empty;
    public int TotalRegisteredPatients { get; set; }
    public decimal TotalOpdFees { get; set; }
    public decimal TotalIpdFees { get; set; }
    public decimal GrandTotalRevenue { get; set; }
    public int TotalConsultationCount { get; set; }
    public int TotalIpdSettledCount { get; set; }
}

public class DoctorDailyFeeResponse
{
    public int DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public decimal OpdCharge { get; set; }
    public decimal PayoutPercentage { get; set; }
    public int ConsultationCount { get; set; }
    public decimal GrossFeeCollected { get; set; }
    public decimal DoctorNetPayout { get; set; }
    public decimal HospitalShare { get; set; }
}
