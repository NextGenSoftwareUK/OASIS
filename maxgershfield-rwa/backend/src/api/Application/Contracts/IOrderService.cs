namespace Application.Contracts;

/// <summary>
/// Defines the contract for the OrderService, which includes operations for creating orders and checking balances.
/// Implementing classes must provide the business logic for these operations.
/// </summary>
public interface IOrderService
{
    /// <summary>
    /// Creates a new order based on the provided request.
    /// This method processes the creation of an order and returns the response with the result of the operation.
    /// </summary>
    /// <param name="request">The request object containing the details for the order to be created.</param>
    /// <param name="token">A cancellation token to cancel the operation if needed. Default is no cancellation.</param>
    /// <returns>Returns a result with the response containing the created order's details.</returns>
    Task<Result<CreateOrderResponse>> CreateOrderAsync(CreateOrderRequest request, CancellationToken token = default);

    /// <summary>
    /// Checks the balance for the specified order ID.
    /// This method retrieves the current balance for the order, including related financial information.
    /// </summary>
    /// <param name="orderId">The unique identifier of the order for which the balance needs to be checked.</param>
    /// <param name="token">A cancellation token to cancel the operation if needed. Default is no cancellation.</param>
    /// <returns>Returns a result with the response containing the balance details for the order.</returns>
    Task<Result<CheckBalanceResponse>> CheckBalanceAsync(Guid orderId, CancellationToken token = default);
}