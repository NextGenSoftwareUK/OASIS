using System;
using System.Linq;
using Amazon;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

public static class CqlDynamoDbReadSourceRegistration
{
    public static void AddDynamoDbReadSource(this IServiceCollection services, IConfiguration configuration)
    {
        const string prefix = "CQL_DYNAMODB_QUERY_";
        var region = configuration[prefix + "REGION"];
        var accessKey = configuration[prefix + "ACCESS_KEY_ID"];
        var secretKey = configuration[prefix + "SECRET_ACCESS_KEY"];
        var sessionToken = configuration[prefix + "SESSION_TOKEN"];
        var probe = configuration[prefix + "PROBE_ID"];
        if (region == null && accessKey == null && secretKey == null && sessionToken == null && probe == null)
            return;

        if (!RegionEndpoint.EnumerableAllRegions.Any(endpoint => endpoint.SystemName == region) ||
            !ValidCredential(accessKey, 128) || !ValidCredential(secretKey, 256) ||
            (sessionToken != null && !ValidCredential(sessionToken, 16384)) ||
            !Guid.TryParseExact(probe, "D", out var id) || id == Guid.Empty)
            throw new InvalidOperationException("DynamoDB CQL requires a recognized AWS region, explicit restricted credentials and an existing probe.");

        services.AddSingleton<CqlReadSourceProfile>(_ => new CqlReadSourceProfile(
            new DynamoDbQuerySource(region, id, accessKey, secretKey, sessionToken)));
    }

    private static bool ValidCredential(string value, int maximumLength) =>
        !string.IsNullOrEmpty(value) && value.Length <= maximumLength &&
        !value.Any(character => char.IsWhiteSpace(character) || char.IsControl(character));
}
