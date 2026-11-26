using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers;

/// <summary>SQL-like and free-text CQL. CqlTextMiddleware compiles the request before usage admission.</summary>
public sealed partial class CqlV1Controller
{
    /// <summary>Runs a SQL-like CQL statement, e.g. SELECT * FROM MongoDBOASIS LIMIT 5.</summary>
    [HttpPost("sql")]
    [RequestSizeLimit(CqlTextMiddleware.MaximumSqlBytes)]
    public Task<IActionResult> Sql([FromBody] CqlSqlRequest request) => RunTextAsync();

    /// <summary>Drafts CQL from a plain-language question using WEB6, and runs it unless execute is false.</summary>
    [HttpPost("ask")]
    [RequestSizeLimit(CqlTextMiddleware.MaximumAskBytes)]
    public Task<IActionResult> Ask([FromBody] CqlAskRequest request) => RunTextAsync();

    private async Task<IActionResult> RunTextAsync()
    {
        if (HttpContext.Items[CqlTextMiddleware.OutcomeKey] is not CqlTextOutcome outcome)
            return TextFailure(401, "CQL_AUTHENTICATION_REQUIRED", "Authenticate with a WEB4 bearer token.");
        if (outcome.Query == null) return TextFailure(outcome.Status, outcome.Code, outcome.Message);
        Response.Headers["X-OASIS-CQL-Parse-Ms"] = outcome.ParseMilliseconds.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
        if (!outcome.Execute)
            return Ok(new NextGenSoftware.OASIS.Common.OASISResult<object>(new { sql = outcome.Sql }));

        Response.Headers["X-OASIS-CQL-Path"] = outcome.Query.Path;
        if (outcome.Query.ScalarAlias != null) Response.Headers["X-OASIS-CQL-Scalar-Alias"] = outcome.Query.ScalarAlias;
        if (HttpContext.Request.RouteValues["action"] as string == nameof(Ask))
            Response.Headers["X-OASIS-CQL-Text"] = Uri.EscapeDataString(outcome.Sql);

        return outcome.Query.Path switch
        {
            "data/query-cql" => await Typed<CqlV1Request>(outcome, r => CqlV1Endpoint.ExecuteAsync(HttpContext, r, providers)),
            "data/query-cql/scalar" => await Typed<CqlScalarV1Request>(outcome, r => CqlV1Endpoint.ExecuteScalarAsync(HttpContext, r, providers)),
            "data/query-cql/projection" => await Typed<CqlProjectionV1Request>(outcome, r => CqlV1Endpoint.ExecuteProjectionAsync(HttpContext, r, providers)),
            "data/query-cql/projection/mixed" => await Typed<CqlProjectionV1Request>(outcome, r => CqlV1Endpoint.ExecuteProjectionAsync(HttpContext, r, providers, mixed: true)),
            "data/query-cql/grouped" => await Typed<CqlGroupedV1Request>(outcome, r => CqlV1Endpoint.ExecuteGroupedAsync(HttpContext, r, providers)),
            "data/query-cql/set" => await Typed<CqlSetV1Request>(outcome, r => CqlV1Endpoint.ExecuteSetAsync(HttpContext, r, providers)),
            "data/query-cql/join" => await Typed<CqlJoinV1Request>(outcome, r => CqlV1Endpoint.ExecuteJoinAsync(HttpContext, r, providers)),
            "data/query-cql/composition" => await Typed<CqlCompositionV1Request>(outcome, r => CqlV1Endpoint.ExecuteCompositionAsync(HttpContext, r, providers)),
            "data/query-cql/federation" => await Typed<CqlFederationV1Request>(outcome, r => CqlV1Endpoint.ExecuteFederationAsync(HttpContext, r, providers)),
            _ => TextFailure(400, "CQL_SQL_UNSUPPORTED", "This statement has no WEB4 query endpoint.")
        };
    }

    // Binds the compiled body exactly as the typed action would, including model validation.
    private async Task<IActionResult> Typed<T>(CqlTextOutcome outcome, Func<T, Task<IActionResult>> execute) where T : class
    {
        T request;
        try
        {
            var options = HttpContext.RequestServices.GetRequiredService<IOptions<JsonOptions>>().Value.JsonSerializerOptions;
            request = outcome.Query.Body.Deserialize<T>(options);
        }
        catch (JsonException) { return TextFailure(400, "CQL_SQL_INVALID", "The compiled query is outside the typed request limits."); }
        if (!TryValidateModel(request)) return ValidationProblem(ModelState);
        return await execute(request);
    }

    private static IActionResult TextFailure(int status, string code, string message) => new ObjectResult(
        new NextGenSoftware.OASIS.Common.OASISResult<object> { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message })
        { StatusCode = status };
}
