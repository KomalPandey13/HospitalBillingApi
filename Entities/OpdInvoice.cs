using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HospitalBillingApi.Entities;

public class OpdInvoice
{
    public int Id { get; set; }

    [Required]
    [MaxLength(30)]
    public string InvoiceNumber { get; set; } = string.Empty;

    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public int DoctorId { get; set; }
    public Doctor Doctor { get; set; } = null!;

    public DateTime BillDate { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Range(0, 1000000)]
    public decimal FeeAmount { get; set; }

    [Required]
    [MaxLength(20)]
    public string PaymentMode { get; set; } = "Cash"; // Cash, UPI, Card

    [Required]
    [MaxLength(20)]
    public string PaymentStatus { get; set; } = "Paid"; // Paid, Refunded

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
