namespace API.Controllers.V1;

/// <summary>
/// Controller that handles order-related operations, including creating orders and checking their balances.
/// Provides endpoints for interacting with the order service for business logic and data management.
/// </summary>
[Route($"{ApiAddresses.Base}/orders")]
public sealed class OrderController(IOrderService orderService) : V1BaseController
{
    /// <summary>
    /// Creates a new order with the provided request details.
    /// </summary>
    /// <param name="request">The request containing the details to create a new order.</param>
    /// <param name="token">Cancellation token to cancel the operation if needed.</param>
    /// <returns>A response indicating the success or failure of the order creation process.</returns>
    [HttpPost]
    public async Task<IActionResult> CreateOrderAsync([FromBody] CreateOrderRequest request,
        CancellationToken token = default)
        => (await orderService.CreateOrderAsync(request, token)).ToActionResult();

    /// <summary>
    /// Checks the balance of an existing order identified by its unique ID.
    /// </summary>
    /// <param name="orderId">The unique identifier of the order to check the balance for.</param>
    /// <param name="token">Cancellation token to cancel the operation if needed.</param>
    /// <returns>A response containing the balance information of the specified order.</returns>
    [HttpGet("{orderId:guid}/check-balance")]
    public async Task<IActionResult> CheckOrderBalance([FromRoute] Guid orderId, CancellationToken token = default)
        => (await orderService.CheckBalanceAsync(orderId, token)).ToActionResult();
}