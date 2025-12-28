namespace Application.Filters;

public sealed record RwaTokenFilter(
    NftAssetType? AssetType,
    decimal? PriceMin,
    decimal? PriceMax,
    SortBy? SortBy,
    SortOrder? SortOrder) : BaseFilter;

public enum SortBy
{
    Price,
    CreatedAt
}

public enum SortOrder
{
    Asc,
    Desc
}