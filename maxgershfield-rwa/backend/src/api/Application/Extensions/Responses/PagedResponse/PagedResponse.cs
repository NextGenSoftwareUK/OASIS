namespace Application.Extensions.Responses.PagedResponse;

/// <summary>
/// Represents a paginated response structure for returning data in a paged format.
/// This class contains the pagination metadata (total pages, total records) and the actual data for the current page.
/// </summary>
/// <typeparam name="T">The type of the data being returned in the paginated response.</typeparam>
public record PagedResponse<T> : BaseFilter
{
    /// <summary>
    /// The total number of pages available based on the pagination parameters.
    /// </summary>
    public int TotalPages { get; init; }

    /// <summary>
    /// The total number of records available across all pages.
    /// </summary>
    public int TotalRecords { get; init; }

    /// <summary>
    /// The data for the current page of the response.
    /// </summary>
    public T? Data { get; init; }

    /// <summary>
    /// Private constructor to initialize the PagedResponse with pagination metadata and data.
    /// </summary>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="pageNumber">The current page number.</param>
    /// <param name="totalRecords">The total number of records across all pages.</param>
    /// <param name="data">The data for the current page.</param>
    private PagedResponse(int pageSize, int pageNumber, int totalRecords, T? data)
        : base(pageSize, pageNumber)
    {
        Data = data;
        TotalRecords = totalRecords;
        TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize);
    }

    /// <summary>
    /// Creates a new instance of the PagedResponse with the provided pagination metadata and data.
    /// </summary>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="pageNumber">The current page number.</param>
    /// <param name="totalRecords">The total number of records across all pages.</param>
    /// <param name="data">The data for the current page.</param>
    /// <returns>A new instance of PagedResponse with the specified pagination information and data.</returns>
    public static PagedResponse<T> Create(int pageSize, int pageNumber, int totalRecords, T? data)
        => new(pageSize, pageNumber, totalRecords, data);
}