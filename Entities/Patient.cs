using System.ComponentModel.DataAnnotations;

namespace HospitalBillingApi.Entities;

public class Patient
{
    public int Id { get; set; }

    [Required]
    [MaxLength(30)]
    public string UHID { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Range(0, 130)]
    public int Age { get; set; }

    [Required]
    [MaxLength(20)]
    public string Gender { get; set; } = string.Empty;

    [MaxLength(25)]
    public string ContactNumber { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<OpdInvoice> OpdInvoices { get; set; } = new List<OpdInvoice>();
    public ICollection<IpdAdmission> IpdAdmissions { get; set; } = new List<IpdAdmission>();
}
