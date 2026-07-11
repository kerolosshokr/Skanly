// Skanly.Application/Common/Models/ServiceResult.cs
namespace Skanly.Application.Common.Models;

/// <summary>
/// Wrapper returned by all Application services.
/// Controllers check IsSuccess before using Data.
/// Eliminates try/catch boilerplate in controllers.
/// </summary>
public class ServiceResult
{
    public bool IsSuccess { get; protected set; }
    public string? ErrorMessage { get; protected set; }
    public IReadOnlyList<string> Errors { get; protected set; } = new List<string>();

    protected ServiceResult() { }

    public static ServiceResult Success()
        => new() { IsSuccess = true };

    public static ServiceResult Failure(string error)
        => new() { IsSuccess = false, ErrorMessage = error };

    public static ServiceResult Failure(IReadOnlyList<string> errors)
        => new() { IsSuccess = false, Errors = errors, ErrorMessage = errors.FirstOrDefault() };
}

public class ServiceResult<T> : ServiceResult
{
    public T? Data { get; private set; }

    private ServiceResult() { }

    public static ServiceResult<T> Success(T data)
        => new() { IsSuccess = true, Data = data };

    public new static ServiceResult<T> Failure(string error)
        => new() { IsSuccess = false, ErrorMessage = error };

    public new static ServiceResult<T> Failure(IReadOnlyList<string> errors)
        => new() { IsSuccess = false, Errors = errors, ErrorMessage = errors.FirstOrDefault() };
}