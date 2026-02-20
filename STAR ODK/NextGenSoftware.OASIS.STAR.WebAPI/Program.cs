using NextGenSoftware.OASIS.STAR.DNA;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Exceptions;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.OASISBootLoader;
using Microsoft.AspNetCore.Mvc.Filters;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Middleware;
using NextGenSoftware.OASIS.STAR.WebAPI.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Ensure OASIS_DNA.json is resolved from the app output directory in local runs.
Directory.SetCurrentDirectory(AppContext.BaseDirectory);

// Configuration for exception handling mode
// Set environment variable ENABLE_GENERIC_EXCEPTION_HANDLING=false to disable generic catch-all in dev/test
var enableGenericExceptionHandling = builder.Configuration.GetValue<bool>("EnableGenericExceptionHandling", 
    bool.Parse(Environment.GetEnvironmentVariable("ENABLE_GENERIC_EXCEPTION_HANDLING") ?? "true"));

// Add services to the container.
builder.Services.AddControllers(options =>
{
    // Add exception filter to catch model binding and other exceptions
    options.Filters.Add<ExceptionFilter>();
})
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.MaxDepth = 64;
        options.JsonSerializerOptions.WriteIndented = false;
    });
builder.Services.AddHttpClient();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Contact = new OpenApiContact()
        {
            Email = "ourworld@nextgensoftware.co.uk",
            Name = "STAR API"
        },
        Description = @"The STAR API that powers the STAR ODK (Software Development Kit) for building advanced applications on top of the OASIS API. Check out <a target='_blank' href='https://drive.google.com/file/d/1nnhGpXcprr6kota1Y85HDDKsBfJHN6sn/view?usp=sharing'>The POWER Of The OASIS API</a> for more info.

To use the STAR API follow these steps: 

 <ol><li>First you need to create your avatar using the avatar/register method.</li><li>You will then receive an email to confirm your address with a token. You then need to call the avatar/verify-email method with this token to verify and activate your new avatar.</li><li>Now you can call the avatar/authenticate method to login and authenticate. This will return your avatar object, which will contain a JWT (JSON Web Token) Security Token.</li><li>You can then set this in your HEADER for all future API calls. See descriptions below for each method for more details on how to use the STAR API...</li></ol>

You will note that every request below has a corresponding overload that also takes a providerType. This allows you to overwrite the default provider configured for the ONODE you are making the request from. The ONODE can be configured to have a list of default providers, which it will fail over to the next one if that provider goes down/is too slow, etc. It will automatically switch to the fastest provider available (and load balance between them) but if the overload is used it will override this default behaviour. Set the setGlobal flag to false if you wish to override only for that given request or to true if you wish to persist this override for all subsequent calls. The current list of providers supported are as follows (in order of priority used):

<ul>
<li><b>MongoDBOASIS</b> - MongoDB Provider (Document/Object Database).</li>
<li><b>SQLLiteDBOASIS</b> - SQLLite Provider (Relational Database).</li>
<li><b>Neo4jOASIS</b> - Neo4j Provider (Graph Database).</li>
<li><b>LocalFileOASIS</b> - Local File Storage Provider.</li>
<li><b>AzureCosmosDBOASIS</b> - Azure COSMOS DB Provider (MS Cloud).</li>
<li><b>GoogleCloudOASIS</b> - Google Cloud Provider.</li>
<li><b>AWSOASIS</b> - Amazon Web Services Provider.</li>
<li><b>IPFSOASIS</b> - IPFS Provider.</li>
<li><b>PinataOASIS</b> - Pinata IPFS Provider.</li>
<li><b>HoloOASIS</b> - Holochain Provider.</li>
<li><b>UrbitOASIS</b> - Urbit Provider.</li>
<li><b>ThreeFoldOASIS</b> - ThreeFold Provider.</li>
<li><b>SOLIDOASIS</b> - SOLID (Social Linked Data) Provider.</li>
<li><b>ActivityPubOASIS</b> - ActivityPub Provider.</li>
<li><b>EthereumOASIS</b> - Ethereum Provider.</li>
<li><b>ArbitrumOASIS</b> - Arbitrum Provider.</li>
<li><b>OptimismOASIS</b> - Optimism Provider.</li>
<li><b>PolygonOASIS</b> - Polygon Provider.</li>
<li><b>BaseOASIS</b> - Base (Coinbase L2) Provider.</li>
<li><b>AvalancheOASIS</b> - Avalanche Provider.</li>
<li><b>BNBChainOASIS</b> - BNB Smart Chain Provider.</li>
<li><b>FantomOASIS</b> - Fantom Provider.</li>
<li><b>SolanaOASIS</b> - Solana Provider.</li>
<li><b>CardanoOASIS</b> - Cardano Provider.</li>
<li><b>PolkadotOASIS</b> - Polkadot Provider.</li>
<li><b>BitcoinOASIS</b> - Bitcoin Provider.</li>
<li><b>NEAROASIS</b> - NEAR Provider.</li>
<li><b>SuiOASIS</b> - Sui Provider.</li>
<li><b>AptosOASIS</b> - Aptos Provider.</li>
<li><b>CosmosBlockChainOASIS</b> - Cosmos SDK/IBC Provider.</li>
<li><b>EOSIOOASIS</b> - EOSIO Provider.</li>
<li><b>TelosOASIS</b> - Telos Provider.</li>
<li><b>SEEDSOASIS</b> - SEEDS Provider.</li>
</ul>

The list above reflects the latest integrated providers. Please check the GitHub repo link below for more details and updates.

The STAR API provides advanced functionality for building sophisticated applications including Celestial Bodies, Spaces, Missions, Quests, Libraries, Templates, and much more. All functionality is built on top of the robust OASIS API foundation.

<b>Please <a target='_blank' href='https://oasisweb4.one/postman/STAR_API.postman_collection.json'>download the Postman JSON file</a> and import it into <a href='https://www.postman.com/' target='_blank'>Postman</a> if you wish to have a play/test and get familiar with the STAR API before plugging it into your application.<br><br> You can download the Postman Dev Environment files below:<br><br><a target='_blank' href='https://oasisweb4.one/postman/STAR_API_DEV.postman_environment.json'>Postman DEV Environment JSON</a><br><a target='_blank' href='https://oasisweb4.one/postman/STAR_API_STAGING.postman_environment.json'>Postman STAGING Environment JSON</a><br><a target='_blank' href='https://oasisweb4.one/postman/STAR_API_LIVE.postman_environment.json'>Postman LIVE Environment JSON</a><br>

This project is Open Source and if you have any feedback or better still, wish to get involved we would love to hear from you, please contact us on <a target='_blank' href='https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK'>GitHub</a>, <a target='_blank' href='https://t.me/ourworldthegamechat'>Telegram</a>, <a target='_blank' href='https://discord.gg/RU6Z8YJ'>Discord</a> or using the <a href='mailto:ourworld@nextgensoftware.co.uk'>Contact</a> link below, we look forward to hearing from you...</b>

<b>If you wish to receive FREE training on how to code and still get to help build a better world with us then please sign up at <a target='_blank' href='https://www.thejusticeleagueaccademy.icu/'>The Justice League Academy</a>. This is a superhero training platform that enables you to unleash your inner superhero and <b>FULL POTENTAL!</b> We <b>BELEIVE</b> in <b>YOU</b> and we will help you find your gift for the world...</b>

<b>Check out the <a href='https://drive.google.com/file/d/1QPgnb39fsoXqcQx_YejdIhhoPbmSuTnF/view?usp=sharing' target='_blank'>DEV Plan/Roadmap</a> to see what has already been built and what is left to be built.</b>

<b>Please join the <a target='_blank' href='https://t.me/oasisapihackalong'>OASIS API Weekly Hackalong Telegram Group</a> if you wish to get the latest news and developments as well as take part in weekly hackalongs where we can help you get up to speed ASAP.</b>

<b>Please consider giving a <a target='_blank' href='http://www.gofundme.com/ourworldthegame'>donation</a> to help keep this vital project going... thank you.</b>



<br><b><b>Want to make a difference in the world?

What will be your legacy?

Ready to be a hero?</b>



<br>Please come join the Our World Tribe on <a href='https://t.me/ourworldthegamechat'>Telegram</a> or <a href='https://discord.gg/q9gMKU6'>Discord.</a>, we look forward to seeing you there... :)</b><b><b>

TOGETHER WE CAN CREATE A BETTER WORLD...</b></b>

<br><a href='https://github.com/dellamsOmega/OASIS/blob/master/STAR%20ODK/NextGenSoftware.OASIS.STAR.WebAPI/STAR%20API%20RELEASE%20HISTORY.md'>Release History</a>

<br><b>Links</b>

<a href='http://www.ourworldthegame.com'>http://www.ourworldthegame.com</a><br><a href='http://www.nextgensoftware.co.uk'>http://www.nextgensoftware.co.uk</a><br><a href='http://www.yoga4autism.com'>http://www.yoga4autism.com</a><br><a href='https://www.thejusticeleagueaccademy.icu/'>https://www.thejusticeleagueaccademy.icu/</a>

<a href='https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK'>https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK</a><br><a href='http://www.gofundme.com/ourworldthegame'>http://www.gofundme.com/ourworldthegame</a>

<a href='https://drive.google.com/file/d/1QPgnb39fsoXqcQx_YejdIhhoPbmSuTnF/view?usp=sharing'>DEV Plan/Roadmap</a><br><a href='https://drive.google.com/file/d/1nnhGpXcprr6kota1Y85HDDKsBfJHN6sn/view?usp=sharing'>The POWER Of The OASIS API</a><br>

<a href='https://drive.google.com/file/d/1b_G08UTALUg4H3jPlBdElZAFvyRcVKj1/view?usp=sharing'>Join The Our World Tribe (Dev Requirements)</a><br><a href='https://drive.google.com/file/d/12pCk20iLw_uA1yIfojcP6WwvyOT4WRiO/view?usp=sharing'>The Our World Mission/Summary</a>

<a href='http://www.facebook.com/ourworldthegame'>http://www.facebook.com/ourworldthegame</a><br><a href='http://www.twitter.com/ourworldthegame'>http://www.twitter.com/ourworldthegame</a><br><a href='https://www.youtube.com/channel/UC0_O4RwdY3lq1m3-K-njUxA'>https://www.youtube.com/channel/UC0_O4RwdY3lq1m3-K-njUxA</a>

<a href='https://t.me/ourworldthegamechat'>https://t.me/ourworldthegamechat</a> (Telegram General Chat)<br><a href='https://t.me/ourworldthegame'>https://t.me/ourworldthegame</a> (Telegram Our World Announcements)<br><a href='https://t.me/ourworldtechupdates'>https://t.me/ourworldtechupdates</a> (Telegram Our World Tech Updates)<br><a href='https://t.me/oasisapihackalong'>https://t.me/oasisapihackalong</a> OASIS API Weekly Hackalongs

<a href='https://discord.gg/q9gMKU6'>https://discord.gg/q9gMKU6</a>",
        Title = "WEB 5 STAR API v1.0.0",
        Version = "v1",
    });

    // Set the comments path for the Swagger JSON and UI.
    var xmlFiles = new[]
    {
        $"{Assembly.GetExecutingAssembly().GetName().Name}.xml",
        "NextGenSoftware.OASIS.API.Core.xml",
        "NextGenSoftware.OASIS.API.ONODE.Core.xml",
        "NextGenSoftware.OASIS.STAR.DNA.xml"
    };

    foreach (var xml in xmlFiles)
    {
        var path = Path.Combine(AppContext.BaseDirectory, xml);
        if (File.Exists(path))
            c.IncludeXmlComments(path, includeControllerXmlComments: true);
    }
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "WEB 5 STAR API v1.0.0");
    c.ConfigObject.AdditionalItems["syntaxHighlight"] = new Dictionary<string, object>
    {
        ["activated"] = false
    };
});

app.UseHttpsRedirection();
app.UseCors("AllowAll");

// Global exception handler - catch all exceptions, log them, and return real error details
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (System.Text.Json.JsonException ex)
    {
        // Model binding/JSON deserialization errors - these are validation errors (400)
        OASISErrorHandling.HandleError($"JSON deserialization error: {ex.Message}", ex, includeStackTrace: true);
        
        var errorResult = new OASISResult<object>
        {
            IsError = true,
            Exception = ex
        };
        
        if (enableGenericExceptionHandling)
        {
            errorResult.Message = $"Invalid args were passed. Invalid JSON in request body: {ex.Message}";
        }
        else
        {
            errorResult.Message = $"Invalid JSON in request body: {ex.Message}";
            errorResult.DetailedMessage = ex.ToString();
        }
        
        context.Response.StatusCode = 400;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(errorResult));
    }
    catch (OASISException ex)
    {
        // OASIS-specific exceptions - these are validation errors (400)
        OASISErrorHandling.HandleError(ex.Message, ex, includeStackTrace: true);
        
        var errorResult = new OASISResult<object>
        {
            IsError = true,
            Exception = ex
        };
        
        if (enableGenericExceptionHandling)
        {
            errorResult.Message = $"Invalid args were passed. {ex.Message}";
        }
        else
        {
            errorResult.Message = ex.Message;
            errorResult.DetailedMessage = ex.ToString();
        }
        
        context.Response.StatusCode = 400;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(errorResult));
    }
    catch (Exception ex)
    {
        // Always log the error
        OASISErrorHandling.HandleError($"Unexpected error in middleware: {ex.Message}", ex, includeStackTrace: true);
        
        // Check if this is a validation error or real exception
        bool isValidationError = ex.Message.Contains("required", StringComparison.OrdinalIgnoreCase) ||
                                ex.Message.Contains("missing", StringComparison.OrdinalIgnoreCase) ||
                                ex.Message.Contains("invalid", StringComparison.OrdinalIgnoreCase) ||
                                ex.Message.Contains("cannot be null", StringComparison.OrdinalIgnoreCase) ||
                                ex.Message.Contains("AvatarId is required", StringComparison.OrdinalIgnoreCase);
        
        var errorResult = new OASISResult<object>
        {
            IsError = true,
            Exception = ex
        };
        
        if (isValidationError)
        {
            // Validation error - return 400
            if (enableGenericExceptionHandling)
            {
                errorResult.Message = $"Invalid args were passed. {ex.Message}";
            }
            else
            {
                errorResult.Message = ex.Message;
                errorResult.DetailedMessage = ex.ToString();
            }
            context.Response.StatusCode = 400;
        }
        else
        {
            // Real exception - return 500
            if (enableGenericExceptionHandling)
            {
                //errorResult.Message = "Oooops. Sorry something broke, it has been logged and we are looking into it!";
                errorResult.Message = $"Unexpected error: {ex.Message}";
                errorResult.DetailedMessage = ex.ToString();
            }
            else
            {
                errorResult.Message = $"Unexpected error: {ex.Message}";
                errorResult.DetailedMessage = ex.ToString();
            }
            context.Response.StatusCode = 500;
        }
        
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(errorResult));
    }
});

//TODO: Seems to be doing similar job to OASISMiddleware & JwtMiddleware? Do we still need?
app.Use(async (context, next) =>
{
    if (context.Request.Headers.TryGetValue("Authorization", out var authHeaderValue))
    {
        var authHeader = authHeaderValue.ToString();
        if (!string.IsNullOrWhiteSpace(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var token = authHeader["Bearer ".Length..].Trim();
            var avatarId = ParseAvatarIdFromJwt(token);
            if (avatarId != Guid.Empty)
            {
                try
                {
                    // Ensure OASIS is booted before loading avatar
                    if (!OASISBootLoader.IsOASISBooted)
                    {
                        var dnaPath = OASISBootLoader.OASISDNAPath ?? 
                                     Path.Combine(AppContext.BaseDirectory, "OASIS_DNA.json");
                        var bootResult = await OASISBootLoader.BootOASISAsync(dnaPath);
                        if (bootResult.IsError)
                        {
                            // Log but don't fail - avatar loading will fail gracefully
                            OASISErrorHandling.HandleError($"Warning: OASIS boot failed in middleware: {bootResult.Message}");
                        }
                    }

                    // Use async method like WEB4 JWT middleware does
                    var avatarLoadResult = await AvatarManager.Instance.LoadAvatarAsync(avatarId);
                    if (!avatarLoadResult.IsError && avatarLoadResult.Result is not null)
                    {
                        context.Items["Avatar"] = avatarLoadResult.Result;
                    }
                }
                catch (Exception ex)
                {
                    // Best-effort context hydration for local API routes - log but don't fail
                    System.Diagnostics.Debug.WriteLine($"Warning: Failed to load avatar in middleware: {ex.Message}");
                }
            }
        }
    }

    await next();
});
app.UseAuthorization();

app.UseMiddleware<OASISMiddleware>();
app.UseMiddleware<ErrorHandlerMiddleware>();
app.UseMiddleware<JwtMiddleware>();
app.UseMiddleware<SubscriptionMiddleware>();

app.MapControllers();

app.Run();

static Guid ParseAvatarIdFromJwt(string? token)
{
    if (string.IsNullOrWhiteSpace(token))
        return Guid.Empty;

    var parts = token.Split('.');
    if (parts.Length < 2)
        return Guid.Empty;

    try
    {
        var payload = parts[1].Replace('-', '+').Replace('_', '/');
        switch (payload.Length % 4)
        {
            case 2: payload += "=="; break;
            case 3: payload += "="; break;
        }

        var bytes = Convert.FromBase64String(payload);
        using var doc = JsonDocument.Parse(bytes);
        if (doc.RootElement.ValueKind != JsonValueKind.Object)
            return Guid.Empty;

        foreach (var property in doc.RootElement.EnumerateObject())
        {
            if ((string.Equals(property.Name, "id", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(property.Name, "avatarId", StringComparison.OrdinalIgnoreCase)) &&
                property.Value.ValueKind == JsonValueKind.String &&
                Guid.TryParse(property.Value.GetString(), out var parsed))
            {
                return parsed;
            }
        }
    }
    catch
    {
    }

    return Guid.Empty;
}

// Exception filter - catch all exceptions, log them, and return real error details
// Can be disabled in dev/test mode via ENABLE_GENERIC_EXCEPTION_HANDLING=false
public class ExceptionFilter : IExceptionFilter
{
    private readonly bool _enableGenericExceptionHandling;

    public ExceptionFilter(IConfiguration configuration)
    {
        _enableGenericExceptionHandling = configuration.GetValue<bool>("EnableGenericExceptionHandling", 
            bool.Parse(Environment.GetEnvironmentVariable("ENABLE_GENERIC_EXCEPTION_HANDLING") ?? "true"));
    }

    public void OnException(ExceptionContext context)
    {
        var ex = context.Exception;
        
        // Always log the error first
        OASISErrorHandling.HandleError($"Error in {context.ActionDescriptor.DisplayName}: {ex.Message}", ex, includeStackTrace: true);
        
        // Check if this is a validation error (400) or real exception (500)
        bool isValidationError = ex is System.Text.Json.JsonException ||
                                 ex is OASISException ||
                                 ex.Message.Contains("required", StringComparison.OrdinalIgnoreCase) ||
                                 ex.Message.Contains("missing", StringComparison.OrdinalIgnoreCase) ||
                                 ex.Message.Contains("invalid", StringComparison.OrdinalIgnoreCase) ||
                                 ex.Message.Contains("cannot be null", StringComparison.OrdinalIgnoreCase) ||
                                 ex.Message.Contains("AvatarId is required", StringComparison.OrdinalIgnoreCase);
        
        var errorResult = new OASISResult<object>
        {
            IsError = true,
            Exception = ex
        };
        
        if (isValidationError)
        {
            // Validation error - return 400
            if (_enableGenericExceptionHandling)
            {
                errorResult.Message = $"Invalid args were passed to {context.ActionDescriptor.DisplayName}. {ex.Message}";
            }
            else
            {
                errorResult.Message = ex.Message;
                errorResult.DetailedMessage = ex.ToString();
            }
            context.Result = new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(errorResult);
        }
        else
        {
            // Real exception - return 500
            if (_enableGenericExceptionHandling)
            {
                //errorResult.Message = "Oooops. Sorry something broke, it has been logged and we are looking into it!";
                errorResult.Message = $"Unexpected error in {context.ActionDescriptor.DisplayName}: {ex.Message}";
                errorResult.DetailedMessage = ex.ToString();
            }
            else
            {
                errorResult.Message = $"Unexpected error in {context.ActionDescriptor.DisplayName}: {ex.Message}";
                errorResult.DetailedMessage = ex.ToString();
            }
            context.Result = new Microsoft.AspNetCore.Mvc.ObjectResult(errorResult)
            {
                StatusCode = 500
            };
        }
        
        context.ExceptionHandled = true;
    }
}
