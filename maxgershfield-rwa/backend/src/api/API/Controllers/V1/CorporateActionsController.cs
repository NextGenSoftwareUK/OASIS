using API.Controllers.Base;
using Application.Contracts;
using Application.DTOs.CorporateAction.Requests;
using Domain.Constants;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.V1;

/// <summary>
/// Controller for managing corporate actions
/// </summary>
[Route("api/oracle/rwa/corporate-actions")]
[Authorize]
public class CorporateActionsController(ICorporateActionService corporateActionService) : V1BaseController
{
    /// <summary>
    /// Get a specific corporate action by ID
    /// </summary>
    /// <param name="id">Corporate action ID</param>
    /// <param name="token">Cancellation token</param>
    /// <returns>Corporate action details</returns>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetCorporateAction(
        Guid id,
        CancellationToken token = default)
        => (await corporateActionService.GetCorporateActionDtoAsync(id, token)).ToActionResult();

    /// <summary>
    /// Get upcoming corporate actions for a symbol
    /// </summary>
    /// <param name="symbol">Stock ticker symbol (e.g., "AAPL")</param>
    /// <param name="daysAhead">Days to look ahead (default: 30, max: 90)</param>
    /// <param name="token">Cancellation token</param>
    /// <returns>List of upcoming corporate actions</returns>
    [HttpGet("{symbol}/upcoming")]
    public async Task<IActionResult> GetUpcomingCorporateActions(
        string symbol,
        [FromQuery] int daysAhead = 30,
        CancellationToken token = default)
        => (await corporateActionService.GetUpcomingCorporateActionsDtoAsync(symbol, daysAhead, token)).ToActionResult();

    /// <summary>
    /// Get all corporate actions for a symbol with filtering and pagination
    /// </summary>
    /// <param name="symbol">Stock ticker symbol (e.g., "AAPL")</param>
    /// <param name="fromDate">Filter actions from this date (optional)</param>
    /// <param name="toDate">Filter actions to this date (optional)</param>
    /// <param name="type">Filter by action type (optional)</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 20, max: 100)</param>
    /// <param name="token">Cancellation token</param>
    /// <returns>Paginated list of corporate actions</returns>
    [HttpGet("{symbol}")]
    public async Task<IActionResult> GetCorporateActions(
        string symbol,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] CorporateActionType? type,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken token = default)
        => (await corporateActionService.GetCorporateActionsWithPaginationAsync(
            symbol, fromDate, toDate, type, page, pageSize, token)).ToActionResult();

    /// <summary>
    /// Create a new corporate action (admin only)
    /// </summary>
    /// <param name="request">Corporate action creation request</param>
    /// <param name="token">Cancellation token</param>
    /// <returns>Created corporate action</returns>
    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> CreateCorporateAction(
        [FromBody] CreateCorporateActionRequestDto request,
        CancellationToken token = default)
        => (await corporateActionService.CreateCorporateActionAsync(request, token)).ToActionResult();
}

