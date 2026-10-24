using System.ComponentModel.DataAnnotations;

namespace HospitalBillingApi.DTOs.Patients;

public class CreatePatientRequest
{
    [Required(ErrorMessage = "Patient full name is required.")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 150 characters.")]
    public string FullName { get; set; } = string.Empty;

    [Range(0, 130, ErrorMessage = "Age must be between 0 and 130.")]
    public int Age { get; set; }

    [Required(ErrorMessage = "Gender is required.")]
    [RegularExpression("^(Male|Female|Other)$", ErrorMessage = "Gender must be 'Male', 'Female', or 'Other'.")]
    public string Gender { get; set; } = string.Empty;

    [Required(ErrorMessage = "Contact number is required.")]
    [RegularExpression(@"^[0-9+\-\s]{7,20}$", ErrorMessage = "Contact number must be between 7 and 20 digits/characters.")]
    public string ContactNumber { get; set; } = string.Empty;
}
