namespace Application.Filters;

public record RwaTokenOwnerFilter(
    Guid? RwaId) : BaseFilter;