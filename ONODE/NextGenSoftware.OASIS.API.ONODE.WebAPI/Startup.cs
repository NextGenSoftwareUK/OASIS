using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using NextGenSoftware.Logging;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Filters;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Middleware;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Services.Solana;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI
{
    public class Startup
    {
        // Helper method to get a unique display name for types, including generic types
        private static string GetTypeDisplayName(Type type)
        {
            if (!type.IsGenericType)
                return type.Name;
            
            var genericTypeName = type.Name.Split('`')[0];
            var genericArgs = string.Join("", type.GetGenericArguments().Select(arg => GetTypeDisplayName(arg)));
            return $"{genericTypeName}Of{genericArgs}";
        }
        private const string VERSION = "WEB 4 OASIS API v4.4.4";
        //readonly string MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {


            // If you wish to change the logging framework from the default (NLog) then set it below (or just change in OASIS_DNA - prefered way)
            //LoggingManager.CurrentLoggingFramework = LoggingFramework.NLog;

            //services.Configure<OASISSettings>(Configuration.GetSection("OASIS")); // Replaced by OASISConfigManager in OASISMiddleware so shares same codebase to STAR ODK.
            // services.AddMvc();

            // services.AddDbContext<DataContext>();
            //services.AddCors(); //Needed twice? It is below too...
            services.AddControllers(x =>
            {
                x.Filters.Add(typeof(ServiceExceptionInterceptor));
                x.Filters.Add(typeof(Filters.OASISHttpStatusResultFilter));
            })
                .AddJsonOptions(x =>
            {
                x.JsonSerializerOptions.IgnoreNullValues = true;
                x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            });
            services.AddHttpClient();
            services.AddHttpClient("Pinata", c => c.Timeout = TimeSpan.FromSeconds(45));
            services.Configure<NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Telegram.TelegramNftMintOptions>(
                Configuration.GetSection(NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Telegram.TelegramNftMintOptions.SectionName));
            services.AddSingleton<NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.ITokenMetadataByMintService, NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.TokenMetadataByMintService>();
            services.AddSingleton<NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.ISolanaSplTokenBalanceService, NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.SolanaSplTokenBalanceService>();
            services.AddSingleton<NextGenSoftware.OASIS.API.ONODE.WebAPI.Interfaces.ISaintMintRecordService, NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.SaintMintRecordService>();
            services.AddSingleton<NextGenSoftware.OASIS.API.ONODE.WebAPI.Interfaces.IBaptiseSaintMintStore, NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.BaptiseSaintMintStore>();
            services.AddSingleton<NextGenSoftware.OASIS.API.ONODE.WebAPI.Interfaces.IBaptiseRunAuditStore, NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.BaptiseRunAuditStore>();
            services.AddSingleton<NextGenSoftware.OASIS.API.ONODE.WebAPI.Interfaces.ISaintBlessingEligibilityService, NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.SaintBlessingEligibilityService>();
            services.AddSingleton<NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.ITopTokenHoldersService, NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.TopTokenHoldersService>();
            services.AddSingleton<NextGenSoftware.OASIS.API.ONODE.WebAPI.Interfaces.IWalletPfpService, NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.WalletPfpService>();
            services.AddSingleton<NextGenSoftware.OASIS.API.ONODE.WebAPI.Interfaces.ISaintNameStore, NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.SaintNameStore>();
            services.AddSingleton<NextGenSoftware.OASIS.API.ONODE.WebAPI.Interfaces.ISaintClaimCodeStore, NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.SaintClaimCodeStore>();
            services.Configure<NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Saints.HallVerificationNftOptions>(
                Configuration.GetSection(NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Saints.HallVerificationNftOptions.SectionName));
            services.AddSingleton<NextGenSoftware.OASIS.API.ONODE.WebAPI.Interfaces.IHallVerificationNftSentStore, NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.HallVerificationNftSentStore>();
            services.AddSingleton<NextGenSoftware.OASIS.API.ONODE.WebAPI.Interfaces.IHallVerificationNftSenderService, NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.HallVerificationNftSenderService>();
            services.AddHostedService<NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.PumpPortalSaintTradeStreamService>();
            services.AddSingleton<NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.IRecentTokenRecipientsService, NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.RecentTokenRecipientsService>();
            services.AddSingleton<NextGenSoftware.OASIS.API.ONODE.WebAPI.Interfaces.ITelegramFlowStateStore, NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.TelegramFlowStateStore>();
            services.AddSingleton<TelegramNftMintFlowService>();
            services.AddSingleton<NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.DropToHoldersExecutionService>();
            services.AddSingleton<WorldService>();
            services.AddSingleton<QuestRegistryService>();
            services.AddSingleton<QuestProofService>();
            services.AddSingleton<ISubscriptionStore, SubscriptionStoreMongoDb>();
            services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
            services.AddSwaggerGen(c =>
            {
                // Configure custom schema ID resolver to handle duplicate class names and generic types
                c.CustomSchemaIds(type =>
                {
                    try
                    {
                        if (type?.Namespace != null && type.Namespace.Contains("NextGenSoftware.OASIS.API.ONODE.WebAPI.Models"))
                            return $"{type.Name}WebAPI";
                        if (type != null && type.IsGenericType)
                        {
                            var genericTypeName = type.Name.Split('`')[0];
                            var genericArgs = string.Join("", type.GetGenericArguments().Select(arg => GetTypeDisplayName(arg)));
                            return $"{genericTypeName}Of{genericArgs}";
                        }
                        return type?.Name ?? "Object";
                    }
                    catch
                    {
                        return type?.Name ?? "Object";
                    }
                });
                
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Contact = new OpenApiContact()
                    {
                        Email = "ourworld@nextgensoftware.co.uk",
                        Name = "OASIS API"
                    },
                    Description = @"The OASIS API that powers Our World and the satellite apps/games/websites (OAPP's/Moons) that plug into it. Check out <a target='_blank' href='https://drive.google.com/file/d/1nnhGpXcprr6kota1Y85HDDKsBfJHN6sn/view?usp=sharing'>The POWER Of The OASIS API</a> for more info.

To use the OASIS API follow these steps: 

 <ol><li>First you need to create your avatar using the avatar/register method.</li><li>You will then receive an email to confirm your address with a token. You then need to call the avatar/verify-email method with this token to verify and activate your new avatar.</li><li>Now you can call the avatar/authenticate method to login and authenticate. This will return your avatar object, which will contain a JWT (JSON Web Token) Security Token.</li><li>You can then set this in your HEADER for all future API calls. See descriptions below for each method for more details on how to use the OASIS API...</li></ol>

You will note that every request below has a corresponding overload that also takes a providerType. This allows you to overwrite the default provider configured for the ONODE you are making the request from. The ONODE can be configured to have a list of default providers, which it will fail over to the next one if that provider goes down/is too slow, etc. It will automatically switch to the fastest provider available (and load balance between them) but if the overload is used it will override this default behaviour. Set the setGlobal flag to false if you wish to override only for that given request or to true if you wish to persist this override for all subsequent calls. The current list of providers supported are as follows:

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
<li><b>TONSOASIS</b> - TON Provider.</li>
<li><b>ZcashOASIS</b> - Zcash Provider.</li>
<li><b>MidenOASIS</b> - Miden Provider.</li>
<li><b>AztecOASIS</b> - Aztec Provider.</li>
<li><b>MonadOASIS</b> - Monad Provider.</li>
<li><b>RadixOASIS</b> - Radix Provider.</li>
<li><b>StarknetOASIS</b> - Starknet Provider.</li>
<li><b>TelegramOASIS</b> - Telegram Provider.</li>

</ul>

The list above reflects the latest integrated providers. Please check the GitHub repo link below for more details and updates.

The Avatar (complete), half the Karma & half the Provider API's are currently implemented. The rest are coming soon... The SCMS (Smart Contract Management System) API's are completed but need to be refactored with some being removed so these also cannot be used currently. These are currently used for our first business use case, B.E.B (Built Environment Blockchain), a construction platform built on top of the OASIS API. More detailed documentation & future releases coming soon... 

<b>Please <a target='_blank' href='https://api.oasisweb4.com/postman/OASIS_API.postman_collection.json'>download the Postman JSON file</a> and import it into <a href='https://www.postman.com/' target='_blank'>Postman</a> if you wish to have a play/test and get familiar with the OASIS API before plugging it into your website/app/game/service.<br><br> You can download the Postman Dev Environment files below:<br><br><a target='_blank' href='https://api.oasisweb4.com/postman/OASIS_API_DEV.postman_environment.json'>Postman DEV Environment JSON</a><br><a target='_blank' href='https://api.oasisweb4.com/postman/OASIS_API_STAGING.postman_environment.json'>Postman STAGING Environment JSON</a><br><a target='_blank' href='https://api.oasisweb4.com/postman/OASIS_API_LIVE.postman_environment.json'>Postman LIVE Environment JSON</a><br>

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

<br><a href='https://github.com/dellamsOmega/OASIS/blob/master/NextGenSoftware.OASIS.API.ONODE.WebAPI/OASIS%20API%20RELEASE%20HISTORY.md'>Release History</a>

<a href='https://www.ourworldthegame.com/single-post/oasis-api-v0-0-1-altha-live'>v0.0.1 ALTHA</a>

<br><b>Links</b>

<a href='http://www.ourworldthegame.com'>http://www.ourworldthegame.com</a><br><a href='http://www.nextgensoftware.co.uk'>http://www.nextgensoftware.co.uk</a><br><a href='http://www.yoga4autism.com'>http://www.yoga4autism.com</a><br><a href='https://www.thejusticeleagueaccademy.icu/'>https://www.thejusticeleagueaccademy.icu/</a>

<a href='https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK'>https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK</a><br><a href='http://www.gofundme.com/ourworldthegame'>http://www.gofundme.com/ourworldthegame</a>

<a href='https://drive.google.com/file/d/1QPgnb39fsoXqcQx_YejdIhhoPbmSuTnF/view?usp=sharing'>DEV Plan/Roadmap</a><br><a href='https://drive.google.com/file/d/1nnhGpXcprr6kota1Y85HDDKsBfJHN6sn/view?usp=sharing'>The POWER Of The OASIS API</a><br>

<a href='https://drive.google.com/file/d/1b_G08UTALUg4H3jPlBdElZAFvyRcVKj1/view?usp=sharing'>Join The Our World Tribe (Dev Requirements)</a><br><a href='https://drive.google.com/file/d/12pCk20iLw_uA1yIfojcP6WwvyOT4WRiO/view?usp=sharing'>The Our World Mission/Summary</a>

<a href='http://www.facebook.com/ourworldthegame'>http://www.facebook.com/ourworldthegame</a><br><a href='http://www.twitter.com/ourworldthegame'>http://www.twitter.com/ourworldthegame</a><br><a href='https://www.youtube.com/channel/UC0_O4RwdY3lq1m3-K-njUxA'>https://www.youtube.com/channel/UC0_O4RwdY3lq1m3-K-njUxA</a>

<a href='https://t.me/ourworldthegamechat'>https://t.me/ourworldthegamechat</a> (Telegram General Chat)<br><a href='https://t.me/ourworldthegame'>https://t.me/ourworldthegame</a> (Telegram Our World Announcements)<br><a href='https://t.me/ourworldtechupdates'>https://t.me/ourworldtechupdates</a> (Telegram Our World Tech Updates)<br><a href='https://t.me/oasisapihackalong'>https://t.me/oasisapihackalong</a> OASIS API Weekly Hackalongs

<a href='https://discord.gg/q9gMKU6'>https://discord.gg/q9gMKU6</a>",
                    Title = VERSION,
                    Version = "v1",
                });

                // Set the comments path for the Swagger JSON and UI.
                var xmlFiles = new[]
                {
                    $"{Assembly.GetExecutingAssembly().GetName().Name}.xml",
                    "NextGenSoftware.OASIS.API.Core.xml",
                    "NextGenSoftware.OASIS.API.ONODE.Core.xml"
                };

                foreach (var xml in xmlFiles)
                {
                    var path = Path.Combine(AppContext.BaseDirectory, xml);
                    if (System.IO.File.Exists(path))
                        c.IncludeXmlComments(path, includeControllerXmlComments: true);
                }

                // Prevent Swagger 500 when generating schema for HttpResponseMessage-derived types (e.g. OASISHttpResponseMessage)
                c.SchemaFilter<Filters.HttpResponseMessageSchemaFilter>();
            });

            /*
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromSeconds(10);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });*/

            // configure strongly typed settings object
            // services.Configure<AppSettings>(Configuration.GetSection("AppSettings"));

            // configure DI for application services
            // services.AddScoped<IAvatarService, AvatarService>(); // AvatarService is being phased out
            //services.AddScoped<IEmailService, EmailService>();
            // Register ISolanaService with factory method to get dependencies from SolanaOASIS provider
            services.AddScoped<ISolanaService>(serviceProvider =>
            {
                // Get the SolanaOASIS provider instance
                var solanaProvider = NextGenSoftware.OASIS.API.Core.Managers.ProviderManager.Instance
                    .GetProvider(NextGenSoftware.OASIS.API.Core.Enums.ProviderType.SolanaOASIS) 
                    as NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.SolanaOASIS;
                
                if (solanaProvider == null)
                {
                    throw new InvalidOperationException("SolanaOASIS provider is not activated or not available. Please ensure the provider is activated in OASIS_DNA.");
                }
                
                // Try to get the existing _solanaService from the provider first
                var providerType = solanaProvider.GetType();
                var solanaServiceField = providerType.GetField("_solanaService", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (solanaServiceField != null)
                {
                    var existingService = solanaServiceField.GetValue(solanaProvider) as ISolanaService;
                    if (existingService != null)
                    {
                        return existingService;
                    }
                }
                
                // If service doesn't exist, create a new one using the provider's account and RPC client
                var accountField = providerType.GetField("_oasisSolanaAccount", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var rpcClientField = providerType.GetField("_rpcClient", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (accountField == null || rpcClientField == null)
                {
                    throw new InvalidOperationException("Unable to access SolanaOASIS provider internal fields. Provider may need to expose Account and RpcClient.");
                }
                
                var account = accountField.GetValue(solanaProvider) as Solnet.Wallet.Account;
                var rpcClient = rpcClientField.GetValue(solanaProvider) as Solnet.Rpc.IRpcClient;
                
                if (account == null || rpcClient == null)
                {
                    throw new InvalidOperationException("SolanaOASIS provider account or RPC client is null. Provider may not be properly initialized.");
                }
                
                return new SolanaService(account, rpcClient);
            });
            //services.AddScoped<ICargoService, CargoService>();
            //services.AddScoped<INftService, NftService>();
            //services.AddScoped<IOlandService, OlandService>();
            services.AddHttpContextAccessor();

            //services.AddCors(options =>
            //{
            //    options.AddPolicy(MyAllowSpecificOrigins,
            //    builder =>
            //    {
            //        builder.WithOrigins("https://localhost:44371").AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
            //        builder.WithOrigins("https://localhost").AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
            //    });
            //});

            //  services.AddControllers();

            //TODO: Don't think this is used anymore? Take out...
            // configure basic authentication 
            //  services.AddAuthentication("BasicAuthentication")
            //     .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>("BasicAuthentication", null);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        //public void Configure(IApplicationBuilder app, IWebHostEnvironment env, DataContext context)
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            LoggingManager.Log("Starting up The OASIS... (REST API)", LogType.Info);
            LoggingManager.Log("Test Debug", LogType.Debug);
            LoggingManager.Log("Test Info", LogType.Info);
            LoggingManager.Log("Test Warning", LogType.Warning);
            LoggingManager.Log("Test Error", LogType.Error);

            // migrate database changes on startup (includes initial db creation)
            //context.Database.Migrate();

            //            IApplicationBuilder app, IHostingEnvironment env)
            //{
            //                app.UseDeveloperExceptionPage();
            //                app.UseStaticFiles();
            //                app.UseMvcWithDefaultRoute();
            //            }


            // generated swagger json and swagger ui middleware
            // When behind a reverse proxy at /api (e.g. oasisweb4.one/api), set Swagger:BasePath to "/api"
            // so the UI fetches /api/swagger/v1/swagger.json and nginx routes it to this app instead of STAR.
            var swaggerBasePath = (Configuration.GetSection("Swagger").GetValue<string>("BasePath") ?? "").TrimEnd('/');
            var swaggerJsonUrl = string.IsNullOrEmpty(swaggerBasePath)
                ? "/swagger/v1/swagger.json"
                : $"{swaggerBasePath}/swagger/v1/swagger.json";
            app.UseSwagger();
            app.UseSwaggerUI(x => x.SwaggerEndpoint(swaggerJsonUrl, VERSION));

            app.UseSwaggerUI(config =>
            {
                config.ConfigObject.AdditionalItems["syntaxHighlight"] = new Dictionary<string, object>
                {
                    ["activated"] = false
                };
            });


            app.UseDeveloperExceptionPage();
            app.UseStaticFiles();
            // app.UseMvcWithDefaultRoute();

            // Only use HTTPS redirection in production (not in development to avoid self-signed certificate issues)
            if (!string.Equals(env.EnvironmentName, "Development", StringComparison.OrdinalIgnoreCase))
            {
                app.UseHttpsRedirection();
            }

            app.UseRouting();
            //app.UseSession();

            // global cors policy
            app.UseCors(x => x
                .SetIsOriginAllowed(origin => true)
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials());

            //TODO: Was this, check later...
            //app.UseCors(MyAllowSpecificOrigins);

            app.UseAuthorization();

            app.UseMiddleware<OASISMiddleware>();
            app.UseMiddleware<ErrorHandlerMiddleware>();
            app.UseMiddleware<JwtMiddleware>();
            // Subscription/credits API disabled - uncomment to require subscription or prepaid credits per request
            // app.UseMiddleware<SubscriptionMiddleware>();

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });

            //  string dbConn = configuration.GetSection("MySettings").GetSection("DbConnection").Value;

            /*
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");

                //endpoints.MapControllerRoute(name: "phases",
                //    pattern: "phases/",
                //    //pattern: "phases/{*article}",
                //    defaults: new { controller = "SmartContractManagement", action = "GetAllPhases" });
            });*/
        }
    }
}

















//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using Microsoft.AspNetCore.Builder;
//using Microsoft.AspNetCore.Hosting;
//using Microsoft.AspNetCore.HttpsPolicy;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Logging;
//using Microsoft.Extensions.Options;

//namespace NextGenSoftware.OASIS.API.ONODE.WebAPI
//{
//    public class Startup
//    {
//        public Startup(IConfiguration configuration)
//        {
//            Configuration = configuration;
//        }

//        public IConfiguration Configuration { get; }

//        // This method gets called by the runtime. Use this method to add services to the container.
//        public void ConfigureServices(IServiceCollection services)
//        {
//            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
//        }

//        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
//        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
//        {
//            if (env.IsDevelopment())
//            {
//                app.UseDeveloperExceptionPage();
//            }
//            else
//            {
//                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
//                app.UseHsts();
//            }

//            app.UseHttpsRedirection();
//            app.UseMvc();
//        }
//    }
//}
