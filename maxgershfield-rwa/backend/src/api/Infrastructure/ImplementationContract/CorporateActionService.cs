using Application.Contracts;
using Application.DTOs.CorporateAction.Requests;
using Application.DTOs.CorporateAction.Responses;
using Application.Extensions.Mappers;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.ExternalServices.CorporateActions;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.ImplementationContract;

/// <summary>
/// Service for managing corporate actions with multi-source consensus and deduplication
/// </summary>
public sealed class CorporateActionService(
    DataContext dbContext,
    IEnumerable<ICorporateActionDataSource> dataSources,
    ILogger<CorporateActionService> logger) : ICorporateActionService
{
    public async Task<List<CorporateAction>> FetchCorporateActionsAsync(
        string symbol,
        DateTime? fromDate = null,
        CancellationToken token = default)
    {
        logger.LogInformation("Fetching corporate actions for symbol {Symbol} from {Count} data sources",
            symbol, dataSources.Count());

        var allActions = new List<CorporateAction>();
        var tasks = new List<Task<List<CorporateAction>>>();

        // Fetch from all data sources in parallel
        foreach (var dataSource in dataSources)
        {
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    var actions = await dataSource.FetchAllActionsAsync(symbol, fromDate, token);
                    logger.LogInformation("Fetched {Count} actions from {Source} for symbol {Symbol}",
                        actions.Count, dataSource.SourceName, symbol);
                    return actions;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error fetching from {Source} for symbol {Symbol}",
                        dataSource.SourceName, symbol);
                    return new List<CorporateAction>();
                }
            }, token));
        }

        var results = await Task.WhenAll(tasks);
        allActions.AddRange(results.SelectMany(r => r));

        // Apply deduplication and consensus
        var deduplicated = DeduplicateAndVerifyActions(allActions);
        
        logger.LogInformation("Fetched {Total} actions, {Deduplicated} after deduplication for symbol {Symbol}",
            allActions.Count, deduplicated.Count, symbol);

        return deduplicated;
    }

    public async Task<List<CorporateAction>> GetCorporateActionsAsync(
        string symbol,
        DateTime? fromDate = null,
        CancellationToken token = default)
    {
        var query = dbContext.Set<CorporateAction>()
            .Where(ca => ca.Symbol == symbol && !ca.IsDeleted);

        if (fromDate.HasValue)
        {
            query = query.Where(ca => ca.EffectiveDate >= fromDate.Value);
        }

        return await query
            .OrderBy(ca => ca.EffectiveDate)
            .ToListAsync(token);
    }

    public async Task<List<CorporateAction>> GetUpcomingCorporateActionsAsync(
        string symbol,
        int daysAhead = 30,
        CancellationToken token = default)
    {
        var today = DateTime.UtcNow.Date;
        var futureDate = today.AddDays(daysAhead);

        return await dbContext.Set<CorporateAction>()
            .Where(ca => ca.Symbol == symbol
                         && ca.EffectiveDate >= today
                         && ca.EffectiveDate <= futureDate
                         && !ca.IsDeleted)
            .OrderBy(ca => ca.EffectiveDate)
            .ToListAsync(token);
    }

    public async Task<CorporateAction?> GetCorporateActionAsync(
        Guid id,
        CancellationToken token = default)
    {
        return await dbContext.Set<CorporateAction>()
            .FirstOrDefaultAsync(ca => ca.Id == id && !ca.IsDeleted, token);
    }

    public async Task SaveCorporateActionsAsync(
        List<CorporateAction> actions,
        CancellationToken token = default)
    {
        if (actions.Count == 0)
        {
            return;
        }

        logger.LogInformation("Saving {Count} corporate actions to database", actions.Count);

        foreach (var action in actions)
        {
            // Check if action already exists (by Symbol + Type + EffectiveDate)
            var existing = await dbContext.Set<CorporateAction>()
                .FirstOrDefaultAsync(
                    ca => ca.Symbol == action.Symbol
                          && ca.Type == action.Type
                          && ca.EffectiveDate.Date == action.EffectiveDate.Date
                          && !ca.IsDeleted,
                    token);

            if (existing != null)
            {
                // Update existing action
                existing.UpdatedAt = DateTimeOffset.UtcNow;
                
                // If new action is verified and existing is not, mark as verified
                if (action.IsVerified && !existing.IsVerified)
                {
                    existing.IsVerified = true;
                }

                // Update data source if different (for audit trail)
                if (existing.DataSource != action.DataSource)
                {
                    // Could store multiple sources, but for simplicity, keep the most reliable
                    var existingSource = dataSources.FirstOrDefault(ds => ds.SourceName == existing.DataSource);
                    var newSource = dataSources.FirstOrDefault(ds => ds.SourceName == action.DataSource);
                    
                    if (newSource != null && 
                        (existingSource == null || newSource.ReliabilityScore > existingSource.ReliabilityScore))
                    {
                        existing.DataSource = action.DataSource;
                    }
                }

                // Update fields if new data is more complete
                if (action.SplitRatio.HasValue && !existing.SplitRatio.HasValue)
                {
                    existing.SplitRatio = action.SplitRatio;
                }

                if (action.DividendAmount.HasValue && !existing.DividendAmount.HasValue)
                {
                    existing.DividendAmount = action.DividendAmount;
                    existing.DividendCurrency = action.DividendCurrency;
                }

                logger.LogDebug("Updated existing corporate action {Id} for symbol {Symbol}",
                    existing.Id, action.Symbol);
            }
            else
            {
                // Add new action
                action.CreatedAt = DateTimeOffset.UtcNow;
                await dbContext.Set<CorporateAction>().AddAsync(action, token);
                logger.LogDebug("Added new corporate action for symbol {Symbol}, type {Type}, date {Date}",
                    action.Symbol, action.Type, action.EffectiveDate);
            }
        }

        await dbContext.SaveChangesAsync(token);
        logger.LogInformation("Successfully saved corporate actions to database");
    }

    public async Task<bool> ValidateCorporateActionAsync(
        CorporateAction action,
        CancellationToken token = default)
    {
        // Basic validation
        if (string.IsNullOrWhiteSpace(action.Symbol))
        {
            logger.LogWarning("Corporate action validation failed: Symbol is required");
            return false;
        }

        if (action.EffectiveDate == default)
        {
            logger.LogWarning("Corporate action validation failed: EffectiveDate is required");
            return false;
        }

        // Type-specific validation
        switch (action.Type)
        {
            case CorporateActionType.StockSplit:
            case CorporateActionType.ReverseSplit:
                if (!action.SplitRatio.HasValue || action.SplitRatio.Value <= 0)
                {
                    logger.LogWarning("Corporate action validation failed: SplitRatio required for splits");
                    return false;
                }
                break;

            case CorporateActionType.Dividend:
            case CorporateActionType.SpecialDividend:
                if (!action.DividendAmount.HasValue || action.DividendAmount.Value <= 0)
                {
                    logger.LogWarning("Corporate action validation failed: DividendAmount required for dividends");
                    return false;
                }
                break;

            case CorporateActionType.Merger:
            case CorporateActionType.Acquisition:
                if (string.IsNullOrWhiteSpace(action.AcquiringSymbol))
                {
                    logger.LogWarning("Corporate action validation failed: AcquiringSymbol required for mergers");
                    return false;
                }
                if (!action.ExchangeRatio.HasValue || action.ExchangeRatio.Value <= 0)
                {
                    logger.LogWarning("Corporate action validation failed: ExchangeRatio required for mergers");
                    return false;
                }
                break;
        }

        return true;
    }

    /// <summary>
    /// Deduplicate actions from multiple sources and mark as verified when found in multiple sources
    /// </summary>
    private List<CorporateAction> DeduplicateAndVerifyActions(List<CorporateAction> actions)
    {
        if (actions.Count == 0)
        {
            return actions;
        }

        // Group by Symbol + Type + EffectiveDate (same day)
        var grouped = actions
            .GroupBy(ca => new
            {
                ca.Symbol,
                ca.Type,
                EffectiveDate = ca.EffectiveDate.Date
            })
            .ToList();

        var deduplicated = new List<CorporateAction>();

        foreach (var group in grouped)
        {
            var groupActions = group.ToList();
            
            // If found in multiple sources, mark as verified
            var uniqueSources = groupActions.Select(ca => ca.DataSource).Distinct().Count();
            var isVerified = uniqueSources > 1;

            // Select the best action (prefer verified, then highest reliability source)
            var bestAction = groupActions
                .OrderByDescending(ca => ca.IsVerified)
                .ThenByDescending(ca =>
                {
                    var source = dataSources.FirstOrDefault(ds => ds.SourceName == ca.DataSource);
                    return source?.ReliabilityScore ?? 0;
                })
                .First();

            // Mark as verified if found in multiple sources
            if (isVerified)
            {
                bestAction.IsVerified = true;
            }

            // Merge data from other sources if missing
            foreach (var otherAction in groupActions.Where(ca => ca != bestAction))
            {
                if (!bestAction.SplitRatio.HasValue && otherAction.SplitRatio.HasValue)
                {
                    bestAction.SplitRatio = otherAction.SplitRatio;
                }

                if (!bestAction.DividendAmount.HasValue && otherAction.DividendAmount.HasValue)
                {
                    bestAction.DividendAmount = otherAction.DividendAmount;
                    bestAction.DividendCurrency = otherAction.DividendCurrency;
                }

                if (string.IsNullOrWhiteSpace(bestAction.AcquiringSymbol) && 
                    !string.IsNullOrWhiteSpace(otherAction.AcquiringSymbol))
                {
                    bestAction.AcquiringSymbol = otherAction.AcquiringSymbol;
                    bestAction.ExchangeRatio = otherAction.ExchangeRatio;
                }
            }

            deduplicated.Add(bestAction);
        }

        return deduplicated;
    }

    public async Task<Result<PagedResponse<IEnumerable<CorporateActionResponseDto>>>> GetCorporateActionsWithPaginationAsync(
        string symbol,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CorporateActionType? type = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken token = default)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(GetCorporateActionsWithPaginationAsync), date);

        try
        {
            // Validate page size (max 100 as per requirements)
            if (pageSize > 100)
            {
                pageSize = 100;
            }

            if (pageSize < 1)
            {
                pageSize = 20;
            }

            if (page < 1)
            {
                page = 1;
            }

            var query = dbContext.Set<CorporateAction>()
                .Where(ca => ca.Symbol == symbol && !ca.IsDeleted)
                .AsQueryable();

            // Apply date filters
            if (fromDate.HasValue)
            {
                query = query.Where(ca => ca.EffectiveDate >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(ca => ca.EffectiveDate <= toDate.Value);
            }

            // Apply type filter
            if (type.HasValue)
            {
                query = query.Where(ca => ca.Type == type.Value);
            }

            // Get total count before pagination
            int totalCount = await query.CountAsync(token);

            // Apply pagination and ordering
            var corporateActions = await query
                .OrderBy(ca => ca.EffectiveDate)
                .Page(page, pageSize)
                .Select(ca => ca.ToResponseDto())
                .ToListAsync(token);

            var pagedResult = PagedResponse<IEnumerable<CorporateActionResponseDto>>.Create(
                pageSize,
                page,
                totalCount,
                corporateActions);

            logger.OperationCompleted(nameof(GetCorporateActionsWithPaginationAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - date);
            return Result<PagedResponse<IEnumerable<CorporateActionResponseDto>>>.Success(pagedResult);
        }
        catch (Exception ex)
        {
            logger.OperationException(nameof(GetCorporateActionsWithPaginationAsync), ex.Message);
            logger.OperationCompleted(nameof(GetCorporateActionsWithPaginationAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - date);
            return Result<PagedResponse<IEnumerable<CorporateActionResponseDto>>>
                .Failure(ResultPatternError.InternalServerError(ex.Message));
        }
    }

    public async Task<Result<UpcomingCorporateActionListResponseDto>> GetUpcomingCorporateActionsDtoAsync(
        string symbol,
        int daysAhead = 30,
        CancellationToken token = default)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(GetUpcomingCorporateActionsDtoAsync), date);

        try
        {
            // Validate daysAhead (max 90 as per requirements)
            if (daysAhead > 90)
            {
                daysAhead = 90;
            }

            if (daysAhead < 1)
            {
                daysAhead = 30;
            }

            var today = DateTime.UtcNow.Date;
            var futureDate = today.AddDays(daysAhead);

            var upcomingActions = await dbContext.Set<CorporateAction>()
                .Where(ca => ca.Symbol == symbol
                             && ca.EffectiveDate >= today
                             && ca.EffectiveDate <= futureDate
                             && !ca.IsDeleted)
                .OrderBy(ca => ca.EffectiveDate)
                .Select(ca => ca.ToUpcomingResponseDto())
                .ToListAsync(token);

            var response = new UpcomingCorporateActionListResponseDto(
                symbol,
                upcomingActions);

            logger.OperationCompleted(nameof(GetUpcomingCorporateActionsDtoAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - date);
            return Result<UpcomingCorporateActionListResponseDto>.Success(response);
        }
        catch (Exception ex)
        {
            logger.OperationException(nameof(GetUpcomingCorporateActionsDtoAsync), ex.Message);
            logger.OperationCompleted(nameof(GetUpcomingCorporateActionsDtoAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - date);
            return Result<UpcomingCorporateActionListResponseDto>
                .Failure(ResultPatternError.InternalServerError(ex.Message));
        }
    }

    public async Task<Result<CorporateActionResponseDto>> GetCorporateActionDtoAsync(
        Guid id,
        CancellationToken token = default)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(GetCorporateActionDtoAsync), date);

        try
        {
            var corporateAction = await dbContext.Set<CorporateAction>()
                .FirstOrDefaultAsync(ca => ca.Id == id && !ca.IsDeleted, token);

            if (corporateAction == null)
            {
                logger.OperationCompleted(nameof(GetCorporateActionDtoAsync), DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow - date);
                return Result<CorporateActionResponseDto>.Failure(
                    ResultPatternError.NotFound($"Corporate action with ID {id} not found"));
            }

            var responseDto = corporateAction.ToResponseDto();

            logger.OperationCompleted(nameof(GetCorporateActionDtoAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - date);
            return Result<CorporateActionResponseDto>.Success(responseDto);
        }
        catch (Exception ex)
        {
            logger.OperationException(nameof(GetCorporateActionDtoAsync), ex.Message);
            logger.OperationCompleted(nameof(GetCorporateActionDtoAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - date);
            return Result<CorporateActionResponseDto>
                .Failure(ResultPatternError.InternalServerError(ex.Message));
        }
    }

    public async Task<Result<CreateCorporateActionResponseDto>> CreateCorporateActionAsync(
        CreateCorporateActionRequestDto request,
        CancellationToken token = default)
    {
        DateTimeOffset date = DateTimeOffset.UtcNow;
        logger.OperationStarted(nameof(CreateCorporateActionAsync), date);

        try
        {
            // Map DTO to entity
            var corporateAction = request.ToEntity();

            // Validate the corporate action
            if (!await ValidateCorporateActionAsync(corporateAction, token))
            {
                logger.OperationCompleted(nameof(CreateCorporateActionAsync), DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow - date);
                return Result<CreateCorporateActionResponseDto>.Failure(
                    ResultPatternError.BadRequest("Corporate action validation failed"));
            }

            // Check for duplicates
            var existing = await dbContext.Set<CorporateAction>()
                .FirstOrDefaultAsync(
                    ca => ca.Symbol == corporateAction.Symbol
                          && ca.Type == corporateAction.Type
                          && ca.EffectiveDate.Date == corporateAction.EffectiveDate.Date
                          && !ca.IsDeleted,
                    token);

            if (existing != null)
            {
                logger.OperationCompleted(nameof(CreateCorporateActionAsync), DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow - date);
                return Result<CreateCorporateActionResponseDto>.Failure(
                    ResultPatternError.Conflict("A corporate action with the same symbol, type, and effective date already exists"));
            }

            // Set creation timestamp
            corporateAction.CreatedAt = DateTimeOffset.UtcNow;

            // Add to database
            await dbContext.Set<CorporateAction>().AddAsync(corporateAction, token);
            await dbContext.SaveChangesAsync(token);

            var responseDto = corporateAction.ToCreateResponseDto();

            logger.LogInformation("Created corporate action {Id} for symbol {Symbol}",
                corporateAction.Id, corporateAction.Symbol);
            logger.OperationCompleted(nameof(CreateCorporateActionAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - date);

            return Result<CreateCorporateActionResponseDto>.Success(responseDto);
        }
        catch (Exception ex)
        {
            logger.OperationException(nameof(CreateCorporateActionAsync), ex.Message);
            logger.OperationCompleted(nameof(CreateCorporateActionAsync), DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow - date);
            return Result<CreateCorporateActionResponseDto>
                .Failure(ResultPatternError.InternalServerError(ex.Message));
        }
    }
}

