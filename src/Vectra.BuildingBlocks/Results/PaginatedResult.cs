using Vectra.BuildingBlocks.Errors;

namespace Vectra.BuildingBlocks.Results;

public class PaginatedResult<T> : Result
{
    public IReadOnlyList<T> Items { get; }
    public int PageNumber { get; }
    public int PageSize { get; }
    public int TotalCount { get; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;

    private PaginatedResult(IReadOnlyList<T> items, int pageNumber, int pageSize, int totalCount)
        : base(true, null)
    {
        Items = items;
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalCount = totalCount;
    }

    private PaginatedResult(Error error)
        : base(false, error)
    {
        Items = [];
    }

    public static PaginatedResult<T> Success(IReadOnlyList<T> items, int pageNumber, int pageSize, int totalCount)
        => new(items, pageNumber, pageSize, totalCount);

    public static Task<PaginatedResult<T>> SuccessAsync(IReadOnlyList<T> items, int pageNumber, int pageSize, int totalCount)
        => Task.FromResult(Success(items, pageNumber, pageSize, totalCount));

    public static new PaginatedResult<T> Failure(Error error) => new(error);

    public static new Task<PaginatedResult<T>> FailureAsync(Error error)
        => Task.FromResult(Failure(error));
}