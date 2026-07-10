// Skanly.Domain/Exceptions/NotFoundException.cs
namespace Skanly.Domain.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string entityName, object key)
        : base($"Entity \"{entityName}\" with key ({key}) was not found.") { }
}