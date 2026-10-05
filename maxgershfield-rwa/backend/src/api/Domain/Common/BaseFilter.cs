namespace Domain.Common;

/// <summary>
/// Represents the base filter used for paginated queries.
/// This class is designed to provide basic pagination functionality with page size and page number.
/// </summary>
public abstract record BaseFilter
{
    /// <summary>
    /// The number of items to display per page in the paginated query.
    /// </summary>
    public int PageSize { get; init; }

    /// <summary>
    /// The current page number in the paginated query.
    /// </summary>
    public int PageNumber { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseFilter"/> class with default values.
    /// The default page size is 10, and the default page number is 1.
    /// </summary>
    protected BaseFilter()
    {
        PageNumber = 1;
        PageSize = 10;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseFilter"/> class with specified page size and page number.
    /// If the provided page size or page number is less than or equal to 0, default values will be used (page size = 10, page number = 1).
    /// </summary>
    /// <param name="pageSize">
    /// The number of items to display per page. If this value is less than or equal to 0, it will default to 10.
    /// </param>
    /// <param name="pageNumber">
    /// The current page number. If this value is less than or equal to 0, it will default to 1.
    /// </param>
    protected BaseFilter(int pageSize, int pageNumber)
    {
        PageSize = pageSize <= 0 ? 10 : pageSize;
        PageNumber = pageNumber <= 0 ? 1 : pageNumber;
    }
}