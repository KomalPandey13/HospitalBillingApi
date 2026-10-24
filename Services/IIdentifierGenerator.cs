namespace HospitalBillingApi.Services;

public interface IIdentifierGenerator
{
    Task<string> GenerateUhidAsync(DateTime? date = null);
    Task<string> GenerateOpdInvoiceNumberAsync(DateTime? date = null);
    Task<string> GenerateIpdAdmissionNumberAsync(DateTime? date = null);
}
