// Skanly.Application/Common/Models/PagedResult.cs
namespace Skanly.Application.Common.Models;

/// <summary>
/// Standard paged response wrapper used by all list endpoints.
/// </summary>
public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = new List<T>();

    public int TotalCount { get; init; }

    public int PageNumber { get; init; }

    public int PageSize { get; init; }

    public int TotalPages =>
        PageSize == 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);

    public bool HasPreviousPage => PageNumber > 1;

    public bool HasNextPage => PageNumber < TotalPages;

    public static PagedResult<T> Create(
        IReadOnlyList<T> items,
        int totalCount,
        int pageNumber,
        int pageSize)
    {
        return new PagedResult<T>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public static PagedResult<T> Empty(
        int pageNumber = 1,
        int pageSize = 10)
    {
        return Create(
            Array.Empty<T>(),
            0,
            pageNumber,
            pageSize);
    }
}