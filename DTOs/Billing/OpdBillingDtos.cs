using System.ComponentModel.DataAnnotations;

namespace HospitalBillingApi.DTOs.Billing;

public class CreateOpdInvoiceRequest
{
    [Required(ErrorMessage = "PatientId is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "PatientId must be a valid positive integer.")]
    public int PatientId { get; set; }

    [Required(ErrorMessage = "DoctorId is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "DoctorId must be a valid positive integer.")]
    public int DoctorId { get; set; }

    [Required(ErrorMessage = "PaymentMode is required.")]
    [RegularExpression("^(Cash|UPI|Card)$", ErrorMessage = "PaymentMode must be 'Cash', 'UPI', or 'Card'.")]
    public string PaymentMode { get; set; } = "Cash";
}

public class OpdInvoiceResponse
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public int PatientId { get; set; }
    public string PatientUhid { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public int DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public DateTime BillDate { get; set; }
    public decimal FeeAmount { get; set; }
    public string PaymentMode { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
