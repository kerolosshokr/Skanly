// Skanly.Domain/Exceptions/DomainException.cs
namespace Skanly.Domain.Exceptions;

public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}