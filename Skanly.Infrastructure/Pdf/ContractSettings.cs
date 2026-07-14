// Skanly.Infrastructure/Pdf/ContractSettings.cs
namespace Skanly.Infrastructure.Pdf;

public class ContractSettings
{
    public const string SectionName = "ContractSettings";

    public string StoragePath { get; init; } = "contracts";
    public string CompanyNameEn { get; init; } = "Skanly Platform";
    public string CompanyNameAr { get; init; } = "منصة سكانلي";
    public string CompanyAddress { get; init; } = "Cairo, Egypt";
    public string CommercialRegistration { get; init; } = "123456789";
    public string SupportEmail { get; init; } = "support@skanly.com";
    public string SupportPhone { get; init; } = "+20-100-000-0000";
    public string ContractPrefix { get; init; } = "SKL";
    public string PlatformTermsUrl { get; init; } = "https://skanly.com/terms";
}