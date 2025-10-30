namespace FarmManagement.Application.DTOs;

public class PagedResult<T>
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public long TotalCount { get; set; }
    public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
}
