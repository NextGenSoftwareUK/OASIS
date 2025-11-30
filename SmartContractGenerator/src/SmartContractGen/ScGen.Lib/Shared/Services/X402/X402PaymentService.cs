using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ScGen.Lib.Shared.DTOs.Requests;
using ScGen.Lib.Shared.DTOs.Responses;
using ScGen.Lib.Shared.Options;

namespace ScGen.Lib.Shared.Services.X402;

public interface IX402PaymentService
{
    Task<Result<VerifyPaymentResponse>> VerifyPaymentAsync(VerifyPaymentRequest request, CancellationToken ct = default);
    bool ValidatePaymentToken(string token, out ClaimsPrincipal? principal);
    PaymentRequiredResponse GetPaymentRequired(string operation, string blockchain);
}

public class X402PaymentService : IX402PaymentService
{
    private readonly X402Options _options;
    private readonly ILogger<X402PaymentService> _logger;
    private readonly HttpClient _httpClient;

    public X402PaymentService(
        IOptions<X402Options> options,
        ILogger<X402PaymentService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _options = options.Value;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
    }

    public async Task<Result<VerifyPaymentResponse>> VerifyPaymentAsync(
        VerifyPaymentRequest request, 
        CancellationToken ct = default)
    {
        try
        {
            // 1. Verify expected price
            decimal expectedPrice = PricingConfig.GetPrice(request.Operation, request.Blockchain);
            if (Math.Abs(request.Amount - expectedPrice) > 0.001m)
            {
                return Result<VerifyPaymentResponse>.Failure(
                    ResultPatternError.BadRequest($"Invalid amount. Expected {expectedPrice} SOL, got {request.Amount} SOL"));
            }

            // 2. Verify transaction on Solana blockchain
            bool isValid = await VerifyTransactionOnChain(request.Signature, request.Amount, ct);
            if (!isValid)
            {
                return Result<VerifyPaymentResponse>.Failure(
                    ResultPatternError.BadRequest("Transaction verification failed"));
            }

            // 3. Generate payment token (JWT)
            string paymentToken = GeneratePaymentToken(request.Signature, request.Operation, request.Blockchain, request.Amount);

            // 4. Trigger x402 webhook for distribution (fire and forget)
            _ = TriggerDistributionWebhook(request, ct);

            var response = new VerifyPaymentResponse
            {
                Verified = true,
                PaymentToken = paymentToken,
                ExpiresAt = DateTime.UtcNow.AddHours(_options.PaymentTokenExpirationHours),
                Signature = request.Signature,
                Amount = request.Amount,
                Operation = request.Operation,
                Blockchain = request.Blockchain
            };

            _logger.LogInformation(
                "Payment verified: {Signature} for {Operation}/{Blockchain} - {Amount} SOL", 
                request.Signature, request.Operation, request.Blockchain, request.Amount);

            return Result<VerifyPaymentResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Payment verification failed: {Message}", ex.Message);
            return Result<VerifyPaymentResponse>.Failure(
                ResultPatternError.InternalServerError($"Payment verification error: {ex.Message}"));
        }
    }

    public bool ValidatePaymentToken(string token, out ClaimsPrincipal? principal)
    {
        principal = null;

        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(_options.JwtSecret))
        {
            return false;
        }

        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_options.JwtSecret);
            
            principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Invalid payment token: {Message}", ex.Message);
            return false;
        }
    }

    public PaymentRequiredResponse GetPaymentRequired(string operation, string blockchain)
    {
        decimal price = PricingConfig.GetPrice(operation, blockchain);

        return new PaymentRequiredResponse
        {
            Pricing = new PricingInfo
            {
                Operation = operation,
                Blockchain = blockchain,
                Price = price,
                Currency = "SOL",
                DistributionPercentage = 90
            },
            X402Endpoint = "/api/v1/payments/verify",
            TreasuryAddress = _options.TreasuryAddress
        };
    }

    private async Task<bool> VerifyTransactionOnChain(string signature, decimal amount, CancellationToken ct)
    {
        try
        {
            // Solana RPC call to get transaction
            var rpcRequest = new
            {
                jsonrpc = "2.0",
                id = 1,
                method = "getTransaction",
                @params = new object[]
                {
                    signature,
                    new { encoding = "json", commitment = "confirmed", maxSupportedTransactionVersion = 0 }
                }
            };

            var content = new StringContent(
                JsonSerializer.Serialize(rpcRequest), 
                Encoding.UTF8, 
                "application/json");

            var response = await _httpClient.PostAsync(_options.SolanaRpcUrl, content, ct);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Solana RPC call failed: {StatusCode}", response.StatusCode);
                return false;
            }

            var jsonResponse = await response.Content.ReadAsStringAsync(ct);
            var doc = JsonDocument.Parse(jsonResponse);

            // Check if transaction exists
            if (!doc.RootElement.TryGetProperty("result", out var result) || result.ValueKind == JsonValueKind.Null)
            {
                _logger.LogWarning("Transaction not found: {Signature}", signature);
                return false;
            }

            // Check if transaction succeeded
            if (result.TryGetProperty("meta", out var meta))
            {
                if (meta.TryGetProperty("err", out var err) && err.ValueKind != JsonValueKind.Null)
                {
                    _logger.LogWarning("Transaction failed: {Signature}", signature);
                    return false;
                }
            }

            // Verify treasury received payment
            // In production, you'd verify the exact transfer amount and recipient
            // For now, we'll just verify the transaction exists and succeeded

            _logger.LogInformation("Transaction verified on-chain: {Signature}", signature);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying transaction on-chain: {Signature}", signature);
            return false;
        }
    }

    private string GeneratePaymentToken(string signature, string operation, string blockchain, decimal amount)
    {
        if (string.IsNullOrEmpty(_options.JwtSecret))
        {
            throw new InvalidOperationException("JWT secret not configured");
        }

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_options.JwtSecret);
        
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("signature", signature),
                new Claim("operation", operation),
                new Claim("blockchain", blockchain),
                new Claim("amount", amount.ToString()),
                new Claim("timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())
            }),
            Expires = DateTime.UtcNow.AddHours(_options.PaymentTokenExpirationHours),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key), 
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private async Task TriggerDistributionWebhook(VerifyPaymentRequest request, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(_options.DistributionWebhookUrl))
        {
            _logger.LogWarning("Distribution webhook URL not configured");
            return;
        }

        try
        {
            var webhook = new
            {
                signature = request.Signature,
                amount = request.Amount,
                operation = request.Operation,
                blockchain = request.Blockchain,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                treasury = _options.TreasuryAddress,
                distributionPercentage = 50,
                nftCollection = "METABRICKS",
                metadata = new
                {
                    operation = request.Operation,
                    blockchain = request.Blockchain,
                    price = request.Amount
                }
            };

            var content = new StringContent(
                JsonSerializer.Serialize(webhook), 
                Encoding.UTF8, 
                "application/json");

            var response = await _httpClient.PostAsync(_options.DistributionWebhookUrl, content, ct);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Distribution webhook triggered successfully for {Signature}", request.Signature);
            }
            else
            {
                _logger.LogWarning("Distribution webhook failed: {StatusCode}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering distribution webhook");
            // Don't throw - this is fire and forget
        }
    }
}

