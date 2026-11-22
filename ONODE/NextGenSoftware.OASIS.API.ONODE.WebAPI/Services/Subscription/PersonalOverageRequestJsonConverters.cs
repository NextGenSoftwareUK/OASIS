using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using ConsentRequest = NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers.SubscriptionController.TogglePayAsYouGoRequest;
using PaymentRequest = NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers.SubscriptionController.PayOveragesRequest;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;

internal static class PersonalBillingRequestJson
{
    internal static Dictionary<string, JsonElement> Fields(ref Utf8JsonReader reader, params string[] allowed)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
            throw new JsonException("A billing request object is required.");
        var known = new HashSet<string>(allowed, StringComparer.OrdinalIgnoreCase);
        var values = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in document.RootElement.EnumerateObject())
        {
            if (!known.Contains(field.Name) || !values.TryAdd(field.Name, field.Value.Clone()))
                throw new JsonException("Billing fields must be known and unique.");
        }
        return values;
    }

    internal static string OptionalString(Dictionary<string, JsonElement> fields, string name)
    {
        if (!fields.TryGetValue(name, out var field)) return null;
        if (field.ValueKind != JsonValueKind.String)
            throw new JsonException("The billing field " + name + " must be a string.");
        return field.GetString();
    }
}

public sealed class PersonalOverageConsentRequestJsonConverter : JsonConverter<ConsentRequest>
{
    public override ConsentRequest Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
    {
        var fields = PersonalBillingRequestJson.Fields(ref reader, "enabled", "pricingVersion", "pricingSha256", "monthlyLimitUsd");
        if (!fields.TryGetValue("enabled", out var enabled) || enabled.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
            throw new JsonException("An explicit enabled boolean is required.");
        decimal cap = 0;
        if (fields.TryGetValue("monthlyLimitUsd", out var amount) &&
            (amount.ValueKind != JsonValueKind.Number || !amount.TryGetDecimal(out cap)))
            throw new JsonException("The monthly USD cap must be a number.");
        return new ConsentRequest {
            Enabled = enabled.GetBoolean(), MonthlyLimitUsd = cap,
            PricingVersion = PersonalBillingRequestJson.OptionalString(fields, "pricingVersion"),
            PricingSha256 = PersonalBillingRequestJson.OptionalString(fields, "pricingSha256")
        };
    }

    public override void Write(Utf8JsonWriter writer, ConsentRequest value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteBoolean("enabled", value.Enabled);
        if (value.PricingVersion != null) writer.WriteString("pricingVersion", value.PricingVersion);
        if (value.PricingSha256 != null) writer.WriteString("pricingSha256", value.PricingSha256);
        if (value.Enabled || value.MonthlyLimitUsd != 0) writer.WriteNumber("monthlyLimitUsd", value.MonthlyLimitUsd);
        writer.WriteEndObject();
    }
}

public sealed class PersonalPayOveragesRequestJsonConverter : JsonConverter<PaymentRequest>
{
    public override PaymentRequest Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
    {
        var fields = PersonalBillingRequestJson.Fields(ref reader, "billingRequestId", "month", "statementId");
        return new PaymentRequest {
            BillingRequestId = PersonalBillingRequestJson.OptionalString(fields, "billingRequestId"),
            Month = PersonalBillingRequestJson.OptionalString(fields, "month"),
            StatementId = PersonalBillingRequestJson.OptionalString(fields, "statementId")
        };
    }

    public override void Write(Utf8JsonWriter writer, PaymentRequest value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        if (value.BillingRequestId != null) writer.WriteString("billingRequestId", value.BillingRequestId);
        if (value.Month != null) writer.WriteString("month", value.Month);
        if (value.StatementId != null) writer.WriteString("statementId", value.StatementId);
        writer.WriteEndObject();
    }
}
