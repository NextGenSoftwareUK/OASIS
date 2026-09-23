using System.Reflection;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.OpenApi.Models;
using NextGenSoftware.OASIS.OASISBootLoader;
using NextGenSoftware.OASIS.Web9.WebAPI.GraphQL;
using NextGenSoftware.OASIS.Web9.WebAPI.GraphQL.Types;
using NextGenSoftware.OASIS.Web9.WebAPI.GrpcServices;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    var port = int.Parse(Environment.GetEnvironmentVariable("PORT") ?? "8080");
    options.ListenAnyIP(port, o => o.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2);
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

builder.Services.AddGrpc();
builder.Services.AddHttpClient();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Contact = new OpenApiContact { Email = "ourworld@nextgensoftware.co.uk", Name = "WEB9 OASIS Singularity Layer API" },
        Description = $"WEB9 v{OASISBootLoader.WEB9APIVersion} - the Singularity Layer made literal: a live unified status aggregator that probes WEB4-WEB8 and reports back as one - the network observing itself." +
            "<br><a href='https://github.com/dellamsOmega/OASIS/blob/master/WEB9/NextGenSoftware.OASIS.Web9.WebAPI/WEB9%20API%20RELEASE%20HISTORY.md'>Release History</a>",
        Title = string.Concat("WEB9 OASIS Singularity Layer API v", OASISBootLoader.WEB9APIVersion),
        Version = "v1"
    });

    var path = System.IO.Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
    if (File.Exists(path))
        c.IncludeXmlComments(path, includeControllerXmlComments: true);
});

builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddType<UnifiedStatusType>()
    .AddType<LayerStatusType>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var cfg = OASISBootLoader.OASISDNA?.OASIS?.Security?.RateLimiting;
        if (cfg == null || !cfg.Enabled)
            return RateLimitPartition.GetNoLimiter("no-limit");

        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetSlidingWindowLimiter(clientIp,
            _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit          = cfg.RequestsPerWindow,
                Window               = TimeSpan.FromSeconds(cfg.WindowSeconds),
                SegmentsPerWindow    = cfg.WindowSegments,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit           = cfg.QueueLimit
            });
    });

    options.OnRejected = async (ctx, token) =>
    {
        ctx.HttpContext.Response.StatusCode = 429;
        ctx.HttpContext.Response.ContentType = "application/json";
        await ctx.HttpContext.Response.WriteAsync(
            "{\"IsError\":true,\"Message\":\"Too many requests. Please slow down and try again later.\"}", token);
    };
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", string.Concat("WEB9 OASIS Singularity Layer API v", OASISBootLoader.WEB9APIVersion)));

if (!string.Equals(app.Environment.EnvironmentName, "Testing", StringComparison.OrdinalIgnoreCase)
    && !app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.UseCors("AllowAll");
app.UseRateLimiter();
app.UseAuthorization();
app.UseMiddleware<NextGenSoftware.OASIS.Web9.WebAPI.Middleware.JwtMiddleware>();
app.UseMiddleware<NextGenSoftware.OASIS.Web9.WebAPI.Middleware.ApiKeyMiddleware>();
app.UseMiddleware<NextGenSoftware.OASIS.Web9.WebAPI.Middleware.SubscriptionMiddleware>();

app.MapGrpcService<SingularityGrpcService>();
app.MapGraphQL();
app.MapControllers();
app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();
