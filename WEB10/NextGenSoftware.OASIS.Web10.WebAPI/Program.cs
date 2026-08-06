using System.Reflection;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.OpenApi.Models;
using NextGenSoftware.OASIS.OASISBootLoader;
using NextGenSoftware.OASIS.Web10.WebAPI.GrpcServices;

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

builder.Services.AddHttpClient();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Contact = new OpenApiContact { Email = "ourworld@nextgensoftware.co.uk", Name = "WEB10 OASIS Source Layer API" },
        Description = $"WEB10 v{OASISBootLoader.WEB10APIVersion} - The Source / WEB0 made literal: the root source-of-truth endpoint, combining the foundational OASIS runtime/version identity with WEB9's live unified status across WEB4-WEB8." +
            "<br><a href='https://github.com/dellamsOmega/OASIS/blob/master/WEB10/NextGenSoftware.OASIS.Web10.WebAPI/WEB10%20API%20RELEASE%20HISTORY.md'>Release History</a>",
        Title = string.Concat("WEB10 OASIS Source Layer API v", OASISBootLoader.WEB10APIVersion),
        Version = "v1"
    });

    var path = System.IO.Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
    if (File.Exists(path))
        c.IncludeXmlComments(path, includeControllerXmlComments: true);
});

builder.Services.AddGrpc();

builder.Services
    .AddGraphQLServer()
    .AddQueryType<NextGenSoftware.OASIS.Web10.WebAPI.GraphQL.Query>()
    .AddMutationType<NextGenSoftware.OASIS.Web10.WebAPI.GraphQL.Mutation>();

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
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", string.Concat("WEB10 OASIS Source Layer API v", OASISBootLoader.WEB10APIVersion)));

if (!string.Equals(app.Environment.EnvironmentName, "Testing", StringComparison.OrdinalIgnoreCase)
    && !app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.UseCors("AllowAll");
app.UseRateLimiter();
app.UseAuthorization();
app.UseMiddleware<NextGenSoftware.OASIS.Web10.WebAPI.Middleware.JwtMiddleware>();
app.UseMiddleware<NextGenSoftware.OASIS.Web10.WebAPI.Middleware.ApiKeyMiddleware>();

app.MapGrpcService<SourceGrpcService>();
app.MapGraphQL();
app.MapControllers();
app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();
