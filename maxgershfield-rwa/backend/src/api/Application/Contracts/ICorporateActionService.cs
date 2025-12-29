using Application.DTOs.CorporateAction.Requests;
using Application.DTOs.CorporateAction.Responses;

namespace Application.Contracts;

/// <summary>
/// Service for managing corporate actions (splits, dividends, mergers, etc.)
/// </summary>
public interface ICorporateActionService
{
    /// <summary>
    /// Fetch corporate actions from external data sources for a symbol
    /// </summary>
    Task<List<CorporateAction>> FetchCorporateActionsAsync(
        string symbol,
        DateTime? fromDate = null,
        CancellationToken token = default);

    /// <summary>
    /// Get all corporate actions for a symbol from database
    /// </summary>
    Task<List<CorporateAction>> GetCorporateActionsAsync(
        string symbol,
        DateTime? fromDate = null,
        CancellationToken token = default);

    /// <summary>
    /// Get upcoming corporate actions for a symbol
    /// </summary>
    Task<List<CorporateAction>> GetUpcomingCorporateActionsAsync(
        string symbol,
        int daysAhead = 30,
        CancellationToken token = default);

    /// <summary>
    /// Get a specific corporate action by ID
    /// </summary>
    Task<CorporateAction?> GetCorporateActionAsync(
        Guid id,
        CancellationToken token = default);

    /// <summary>
    /// Save corporate actions to database with deduplication
    /// </summary>
    Task SaveCorporateActionsAsync(
        List<CorporateAction> actions,
        CancellationToken token = default);

    /// <summary>
    /// Validate a corporate action
    /// </summary>
    Task<bool> ValidateCorporateActionAsync(
        CorporateAction action,
        CancellationToken token = default);

    /// <summary>
    /// Get corporate actions for a symbol with filtering and pagination
    /// </summary>
    Task<Result<PagedResponse<IEnumerable<CorporateActionResponseDto>>>> GetCorporateActionsWithPaginationAsync(
        string symbol,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CorporateActionType? type = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken token = default);

    /// <summary>
    /// Get upcoming corporate actions for a symbol
    /// </summary>
    Task<Result<UpcomingCorporateActionListResponseDto>> GetUpcomingCorporateActionsDtoAsync(
        string symbol,
        int daysAhead = 30,
        CancellationToken token = default);

    /// <summary>
    /// Get a specific corporate action by ID
    /// </summary>
    Task<Result<CorporateActionResponseDto>> GetCorporateActionDtoAsync(
        Guid id,
        CancellationToken token = default);

    /// <summary>
    /// Create a new corporate action (admin only)
    /// </summary>
    Task<Result<CreateCorporateActionResponseDto>> CreateCorporateActionAsync(
        CreateCorporateActionRequestDto request,
        CancellationToken token = default);
}

