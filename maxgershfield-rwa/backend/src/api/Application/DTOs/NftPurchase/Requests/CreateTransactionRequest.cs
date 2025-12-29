using Newtonsoft.Json;

namespace Application.DTOs.NftPurchase.Requests;

public readonly record struct CreateTransactionRequest(
    [property: JsonProperty("buyerPubkey")]
    string BuyerPubkey,
    [property: JsonProperty("sellerPubkey")]
    string SellerPubkey,
    [property: JsonProperty("sellerSecretkey")]
    string SellerSecretKey,
    [property: JsonProperty("nftMint")] string NftMint,
    [property: JsonProperty("price")] decimal Price,
    [property: JsonProperty("tokenMint")] string? TokenMint);