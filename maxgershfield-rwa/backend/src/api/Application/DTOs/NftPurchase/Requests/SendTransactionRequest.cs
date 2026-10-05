using Newtonsoft.Json;

namespace Application.DTOs.NftPurchase.Requests;

public readonly record struct SendTransactionRequest(
    [property: JsonProperty("signedTransaction")]
    string SignedTransaction);