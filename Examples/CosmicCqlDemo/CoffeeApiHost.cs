using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Any;
using Swashbuckle.AspNetCore.SwaggerGen;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace CosmicCqlDemo;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DemoScope { Default, Named, AllEligible }
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CoffeeOrder { Rating, Price, Name }
public sealed class CoffeeQueryRequest
{
    [StringLength(64)] public string? Category { get; set; }
    public string[]? Categories { get; set; }
    [Range(0, 10)] public int MinimumRating { get; set; }
    [StringLength(128)] public string? City { get; set; }
    public bool InStockOnly { get; set; } = true;
    public CoffeeOrder OrderBy { get; set; } = CoffeeOrder.Rating;
    public bool Descending { get; set; } = true;
    [Range(0, 1000)] public int Skip { get; set; }
    [Range(1, 20)] public int Take { get; set; } = 10;
    public DemoScope Scope { get; set; } = DemoScope.Default;
    public string? Provider { get; set; }
}
public sealed record CoffeeApiRow(Guid Id, string Name, string Category, int Rating, int PriceInPence, int Stock, int Reserved, int Version);
public sealed record CoffeeQueryPage(string Provider, int Count, IReadOnlyList<CoffeeApiRow> Rows);

public sealed class CoffeeQueryExample : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type != typeof(CoffeeQueryRequest)) return;
        schema.Example = new OpenApiObject {
            ["category"] = new OpenApiString("coffee"), ["minimumRating"] = new OpenApiInteger(8),
            ["inStockOnly"] = new OpenApiBoolean(true), ["orderBy"] = new OpenApiString("Price"),
            ["descending"] = new OpenApiBoolean(false), ["scope"] = new OpenApiString("Named"),
            ["provider"] = new OpenApiString("SQLLiteDBOASIS"), ["skip"] = new OpenApiInteger(0), ["take"] = new OpenApiInteger(2)
        };
    }
}

public static class CoffeeApiHost
{
    public static async Task RunAsync(HolonManager manager, ICosmicQueryStorageProvider adapter, string root)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = Array.Empty<string>(), ContentRootPath = root });
        builder.Configuration.Sources.Clear();
        builder.Configuration.AddInMemoryCollection();
        builder.WebHost.UseUrls("http://127.0.0.1:5087");
        builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options => {
            options.SchemaFilter<CoffeeQueryExample>();
            options.SwaggerDoc("v1", new OpenApiInfo {
            Title = "COSMIC ORM / WEB4 local CQL demo", Version = "v1",
            Description = "Real HolonManager and native SQLite CQL over five synthetic coffee records. Local demonstration host; production ONODE authentication/routing is separate."
            });
        });
        await using var app = builder.Build();
        app.UseSwagger();
        app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "COSMIC CQL demo"));
        app.MapGet("/", () => Results.Redirect("/swagger"));
        app.MapGet("/health", () => Results.Ok(new { status = "ready", provider = adapter.QueryProviderType.ToString(), syntheticData = true }));
        app.MapPost("/api/cql/coffees", async (CoffeeQueryRequest request, CancellationToken cancellation) =>
            await ExecuteAsync(request, manager, adapter, cancellation))
            .WithName("QueryCoffees").WithTags("CQL")
            .Produces<OASISResult<CoffeeQueryPage>>(200).Produces<OASISResult<CoffeeQueryPage>>(400);
        app.MapPost("/api/cql/demo/shutdown", (HttpContext context, IHostApplicationLifetime lifetime) => {
            context.Response.OnCompleted(() => { lifetime.StopApplication(); return Task.CompletedTask; });
            return Results.Ok(new { status = "stopping", syntheticData = true });
        }).WithTags("Demo lifecycle");
        Console.WriteLine("\nSwagger demo: http://127.0.0.1:5087/swagger (Ctrl+C stops and cleans the owned fixture).");
        await app.RunAsync();
    }

    private static IResult Failure(string code, string message, int status = 400) => Results.Json(new OASISResult<CoffeeQueryPage> {
        IsError = true, ErrorCount = 1, ErrorCode = code, Message = message
    }, statusCode: status);

    public static async Task<IResult> ExecuteAsync(CoffeeQueryRequest request, HolonManager manager,
        ICosmicQueryStorageProvider adapter, CancellationToken cancellation)
    {
        if (request.MinimumRating < 0 || request.MinimumRating > 10 || request.Skip < 0 || request.Skip > 1000 || request.Take < 1 || request.Take > 20 ||
            request.Category?.Length > 64 || request.City?.Length > 128 || request.Provider?.Length > 128 ||
            !Enum.IsDefined(request.OrderBy) || !Enum.IsDefined(request.Scope) || request.Categories is { Length: > 8 } ||
            request.Categories?.Any(value => string.IsNullOrEmpty(value) || value.Length > 64) == true)
            return Failure("CQL_DEMO_REQUEST_INVALID", "Use rating 0–10, skip 0–1000, take 1–20, bounded strings/categories and defined scope/order values.");
        if (request.Scope != DemoScope.Named && request.Provider != null)
            return Failure("CQL_DEMO_REQUEST_INVALID", "Provider is only valid with Named scope.");
        CosmicQueryScope scope;
        if (request.Scope == DemoScope.Named)
        {
            if (!Enum.TryParse<ProviderType>(request.Provider, false, out var named) || !Enum.IsDefined(named))
                return Failure("CQL_SCOPE_INVALID", "Named scope requires a defined concrete provider name.");
            var selected = CosmicQueryScope.Named(named);
            if (selected.IsError) return Failure(selected.ErrorCode, selected.Message);
            scope = selected.Result;
        }
        else scope = request.Scope == DemoScope.AllEligible ? CosmicQueryScope.AllEligible : CosmicQueryScope.CurrentDefault;
        // Freeze plain values: the expression compiler never evaluates request getters.
        var minimum = request.MinimumRating;
        var built = CosmicQuery<Coffee>.Create(scope).Where(c => c.Rating >= minimum);
        if (request.Category is string category) built = built.Where(c => c.Category == category);
        if (request.Categories is { Length: > 0 } categories) built = built.Where(c => categories.Contains(c.Category));
        if (request.City is string city) built = built.Where(c => CosmicQueryMetadata.Value<string>(c, "location", "city") == city);
        if (request.InStockOnly) built = built.Where(c => c.Stock > c.Reserved);
        built = request.OrderBy switch {
            CoffeeOrder.Price => request.Descending ? built.OrderByDescending(c => c.PriceInPence) : built.OrderBy(c => c.PriceInPence),
            CoffeeOrder.Name => request.Descending ? built.OrderByDescending(c => c.Name) : built.OrderBy(c => c.Name),
            _ => request.Descending ? built.OrderByDescending(c => c.Rating) : built.OrderBy(c => c.Rating)
        };
        built = built.ThenBy(c => c.Name).Skip(request.Skip).Take(request.Take);
        if (built.IsError) return Failure(built.ErrorCode, built.Message);
        var plan = CosmicQueryPlanner.Plan(built.Result, adapter.QueryProviderType,
            new[] { new CosmicQueryTarget(adapter.QueryProviderType, adapter.QueryCapabilities) });
        if (plan.IsError) return Failure(plan.ErrorCode, plan.Message);
        var result = await manager.ExecuteQueryAsync(plan.Result, new[] { adapter }, maximumResults: 20, cancellationToken: cancellation);
        if (result.IsError) return Failure(result.ErrorCode, result.Message, result.ErrorCode == "CQL_QUERY_CANCELLED" ? 408 : 500);
        var group = result.Result.Providers.Single();
        var rows = group.Rows.Select(c => new CoffeeApiRow(c.Id, c.Name, c.Category, c.Rating, c.PriceInPence, c.Stock, c.Reserved, c.Version)).ToArray();
        return Results.Json(new OASISResult<CoffeeQueryPage>(new(group.Provider.ToString(), rows.Length, rows)) { IsLoaded = true });
    }
}
