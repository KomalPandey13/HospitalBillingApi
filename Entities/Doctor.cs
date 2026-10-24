using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HospitalBillingApi.Entities;

public class Doctor
{
    public int Id { get; set; }

    [Required]
    [MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Specialization { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    [Range(0, 1000000)]
    public decimal OpdCharge { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    [Range(0, 100)]
    public decimal PayoutPercentage { get; set; } = 70.0m;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<OpdInvoice> OpdInvoices { get; set; } = new List<OpdInvoice>();
    public ICollection<IpdAdmission> IpdAdmissions { get; set; } = new List<IpdAdmission>();
}
