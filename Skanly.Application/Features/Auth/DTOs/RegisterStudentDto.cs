// Skanly.Application/Features/Auth/DTOs/RegisterStudentDto.cs
namespace Skanly.Application.Features.Auth.DTOs;

public class RegisterStudentDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public byte Gender { get; set; }       // 1=Male, 2=Female
    public int? UniversityId { get; set; }
    public bool AgreeToTerms { get; set; }
}