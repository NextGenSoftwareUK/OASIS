using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ScGen.Lib.Shared.DTOs.Responses;
using ScGen.Lib.Shared.Options;
using ScGen.Lib.Shared.Services.X402;

namespace ScGen.Lib.Shared.Middlewares;

/// <summary>
/// Middleware to enforce x402 payment requirements
/// </summary>
public class X402PaymentMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<X402PaymentMiddleware> _logger;
    private readonly X402Options _options;

    // Endpoints that require payment
    private static readonly string[] PaymentRequiredEndpoints =
    {
        "/api/v1/contracts/generate",
        "/api/v1/contracts/compile",
        "/api/v1/contracts/deploy"
    };

    // Endpoints that are exempt from payment (even if payment is enabled)
    private static readonly string[] ExemptEndpoints =
    {
        "/api/v1/payments",
        "/api/v1/credits",
        "/health",
        "/swagger",
        "/api/v1/nft"
    };

    public X402PaymentMiddleware(
        RequestDelegate next,
        ILogger<X402PaymentMiddleware> logger,
        IOptions<X402Options> options)
    {
        _next = next;
        _logger = logger;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context, IX402PaymentService paymentService, ICreditsService creditsService)
    {
        // Skip if payment not required globally
        if (!_options.RequirePayment)
        {
            await _next(context);
            return;
        }

        // Skip exempt endpoints
        if (IsExemptEndpoint(context.Request.Path))
        {
            await _next(context);
            return;
        }

        // Check if endpoint requires payment
        if (!RequiresPayment(context.Request.Path))
        {
            await _next(context);
            return;
        }

        // Get operation details
        string? operation = GetOperationFromPath(context.Request.Path);
        string? blockchain = GetBlockchainFromRequest(context);
        
        // Try to get wallet address from header
        string? walletAddress = context.Request.Headers["X-Wallet-Address"].FirstOrDefault();
        
        // Check if user has credits (credits take priority over payment tokens)
        if (!string.IsNullOrEmpty(walletAddress) && operation != null && blockchain != null)
        {
            int currentCredits = await creditsService.GetCreditsAsync(walletAddress);
            int requiredCredits = CreditCosts.GetCost(operation, blockchain);
            
            if (currentCredits >= requiredCredits)
            {
                // Deduct credits and proceed
                bool deducted = await creditsService.DeductCreditsAsync(walletAddress, requiredCredits);
                if (deducted)
                {
                    context.Items["PaymentMethod"] = "credits";
                    context.Items["CreditsUsed"] = requiredCredits;
                    context.Items["CreditsRemaining"] = currentCredits - requiredCredits;
                    
                    _logger.LogInformation("Operation authorized via credits: {Wallet} - {Operation}/{Blockchain} - {Credits} credits",
                        walletAddress, operation, blockchain, requiredCredits);
                    
                    await _next(context);
                    return;
                }
            }
        }

        // Extract payment token from header
        if (!context.Request.Headers.TryGetValue("X-Payment-Token", out var paymentToken) ||
            string.IsNullOrEmpty(paymentToken))
        {
            await SendPaymentRequired(context, paymentService, creditsService, walletAddress);
            return;
        }

        // Validate payment token
        if (!paymentService.ValidatePaymentToken(paymentToken!, out var principal))
        {
            await SendPaymentRequired(context, paymentService, creditsService, walletAddress, "Invalid or expired payment token");
            return;
        }

        // Verify operation matches payment token (reuse already declared variables)

        if (operation != null && principal != null)
        {
            var tokenOperation = principal.FindFirst("operation")?.Value;
            var tokenBlockchain = principal.FindFirst("blockchain")?.Value;

            if (tokenOperation != operation || (blockchain != null && tokenBlockchain != blockchain))
            {
                _logger.LogWarning(
                    "Payment token mismatch: token={TokenOp}/{TokenChain}, request={ReqOp}/{ReqChain}",
                    tokenOperation, tokenBlockchain, operation, blockchain);
                
                await SendPaymentRequired(context, paymentService, creditsService, walletAddress, "Payment token does not match operation");
                return;
            }
        }

        // Add payment info to context for logging
        if (principal != null)
        {
            context.Items["PaymentSignature"] = principal.FindFirst("signature")?.Value;
            context.Items["PaymentAmount"] = principal.FindFirst("amount")?.Value;
        }

        _logger.LogInformation("Payment verified for {Path}", context.Request.Path);

        // Continue to next middleware
        await _next(context);
    }

    private bool RequiresPayment(PathString path)
    {
        return PaymentRequiredEndpoints.Any(endpoint => 
            path.StartsWithSegments(endpoint, StringComparison.OrdinalIgnoreCase));
    }

    private bool IsExemptEndpoint(PathString path)
    {
        return ExemptEndpoints.Any(endpoint => 
            path.StartsWithSegments(endpoint, StringComparison.OrdinalIgnoreCase));
    }

    private string? GetOperationFromPath(PathString path)
    {
        string pathString = path.Value?.ToLower() ?? "";
        
        if (pathString.Contains("/generate")) return "generate";
        if (pathString.Contains("/compile")) return "compile";
        if (pathString.Contains("/deploy")) return "deploy";
        
        return null;
    }

    private string? GetBlockchainFromRequest(HttpContext context)
    {
        // Try to get from form data
        if (context.Request.HasFormContentType && context.Request.Form.ContainsKey("Language"))
        {
            return context.Request.Form["Language"].ToString();
        }

        // Try to get from query string
        if (context.Request.Query.ContainsKey("Language"))
        {
            return context.Request.Query["Language"].ToString();
        }

        return null;
    }

    private async Task SendPaymentRequired(
        HttpContext context, 
        IX402PaymentService paymentService,
        ICreditsService creditsService,
        string? walletAddress = null,
        string? customMessage = null)
    {
        string operation = GetOperationFromPath(context.Request.Path) ?? "generate";
        string blockchain = GetBlockchainFromRequest(context) ?? "Rust";

        PaymentRequiredResponse paymentInfo = paymentService.GetPaymentRequired(operation, blockchain);

        // Add credits information
        int currentCredits = 0;
        int requiredCredits = CreditCosts.GetCost(operation, blockchain);
        
        if (!string.IsNullOrEmpty(walletAddress))
        {
            currentCredits = await creditsService.GetCreditsAsync(walletAddress);
        }

        var response = new
        {
            error = customMessage ?? "Payment Required",
            acceptedMethods = new[] { "solana-x402", "credits" },
            pricing = paymentInfo.Pricing,
            x402Endpoint = paymentInfo.X402Endpoint,
            treasuryAddress = paymentInfo.TreasuryAddress,
            credits = new
            {
                required = requiredCredits,
                current = currentCredits,
                packs = CreditsService.CREDIT_PACKS
            }
        };

        context.Response.StatusCode = StatusCodes.Status402PaymentRequired;
        context.Response.Headers.Append("X-Accept-Payment", "solana-x402,credits");
        context.Response.ContentType = "application/json";

        string json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });

        await context.Response.WriteAsync(json);

        _logger.LogInformation(
            "Payment required for {Path}: {Operation}/{Blockchain} - {Price} SOL or {Credits} credits (user has {CurrentCredits})",
            context.Request.Path, operation, blockchain, paymentInfo.Pricing.Price, requiredCredits, currentCredits);
    }
}

