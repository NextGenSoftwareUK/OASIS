using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using NextGenSoftware.Logging;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Filters;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Middleware;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Services.Solana;
using NextGenSoftware.OASIS.API.Providers.TelegramOASIS;
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
        private const string VERSION = "WEB 4 OASIS API v3.3.4";
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
            services.AddControllers(x => x.Filters.Add(typeof(ServiceExceptionInterceptor)))
                .AddJsonOptions(x => x.JsonSerializerOptions.IgnoreNullValues = true);
            services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
            services.AddSwaggerGen(c =>
            {
                // Configure custom schema ID resolver to handle duplicate class names and generic types
                c.CustomSchemaIds(type => 
                {
                    // If the type is from the WebAPI Models namespace, use a different schema ID
                    if (type.Namespace != null && type.Namespace.Contains("NextGenSoftware.OASIS.API.ONODE.WebAPI.Models"))
                    {
                        return $"{type.Name}WebAPI";
                    }
                    
                    // Handle generic types to include the full generic parameter information
                    if (type.IsGenericType)
                    {
                        var genericTypeName = type.Name.Split('`')[0]; // Get the base name (e.g., "EnumValue")
                        var genericArgs = string.Join("", type.GetGenericArguments().Select(arg => GetTypeDisplayName(arg)));
                        return $"{genericTypeName}Of{genericArgs}";
                    }
                    
                    // For all other types, use the default behavior
                    return type.Name;
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

The Avatar (complete), half the Karma & half the Provider API's are currently implemented. The rest are coming soon... The SCMS (Smart Contract Management System) API's are completed but need to be refactored with some being removed so these also cannot be used currently. These are currently used for our first business use case, B.E.B (Built Environment Blockchain), a construction platform built on top of the OASIS API. More detailed documentation & future releases coming soon... 

<b>Please <a target='_blank' href='https://oasisweb4.one/postman/OASIS_API.postman_collection.json'>download the Postman JSON file</a> and import it into <a href='https://www.postman.com/' target='_blank'>Postman</a> if you wish to have a play/test and get familiar with the OASIS API before plugging it into your website/app/game/service.<br><br> You can download the Postman Dev Environment files below:<br><br><a target='_blank' href='https://oasisweb4.one/postman/OASIS_API_DEV.postman_environment.json'>Postman DEV Environment JSON</a><br><a target='_blank' href='https://oasisweb4.one/postman/OASIS_API_STAGING.postman_environment.json'>Postman STAGING Environment JSON</a><br><a target='_blank' href='https://oasisweb4.one/postman/OASIS_API_LIVE.postman_environment.json'>Postman LIVE Environment JSON</a><br>

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
            //services.AddScoped<ISolanaService, SolanaService>(); //TODO: Not sure we need this? Want to remove this along with all other services ASAP! Use Managers in OASIS.API.Core and OASIS.API.ONODE.Core instead!
            //services.AddScoped<ICargoService, CargoService>();
            //services.AddScoped<INftService, NftService>();
            //services.AddScoped<IOlandService, OlandService>();
            
            // FIX: Allow .NET HttpClient to connect to Telegram on macOS
            System.Net.ServicePointManager.ServerCertificateValidationCallback = 
                (sender, certificate, chain, sslPolicyErrors) => true;
            
            // Register Telegram services
            services.AddSingleton<TelegramOASIS>(sp =>
            {
                // Load from OASIS_DNA.json TelegramOASIS config - TIMORIDES BOT - LOCAL POLLING
                string botToken = "8000192131:AAE3DY-AxbnhaPBaLF_mBogV169CeRXGleg";
                string webhookUrl = ""; // Empty for local polling
                // Note: In C# code, use the actual ! character, not URL-encoded %21
                string mongoConnectionString = "mongodb+srv://OASISWEB4:Uppermall1!@oasisweb4.ifxnugb.mongodb.net/?retryWrites=true&w=majority&appName=OASISWeb4";
                
                var provider = new TelegramOASIS(botToken, webhookUrl, mongoConnectionString);
                
                // Activate the provider immediately
                var activationResult = provider.ActivateProvider();
                if (activationResult.IsError)
                {
                    System.Console.WriteLine($"❌ TelegramOASIS activation failed: {activationResult.Message}");
                }
                else
                {
                    System.Console.WriteLine("✅ TelegramOASIS provider activated in DI registration");
                }
                
                return provider;
            });
            
            services.AddSingleton<NFTService>(sp =>
            {
                var logger = sp.GetService<ILogger<NFTService>>();
                return new NFTService(
                    "http://localhost:5000",  // Fixed: Changed from 5003 to 5000 (actual OASIS API port)
                    "max.gershfield1@gmail.com",
                    "Uppermall1!",
                    Guid.Parse("5f7daa80-160e-4213-9e81-94500390f31e"),
                    logger
                );
            });
            
            services.AddSingleton<PinataService>(sp =>
            {
                var logger = sp.GetService<ILogger<PinataService>>();
                return new PinataService(
                    "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VySW5mb3JtYXRpb24iOnsiaWQiOiJmMTg4ODA1Ny0yZDRhLTQ1MzMtOWI4ZS0wZGMxYjEwNmM4YzMiLCJlbWFpbCI6Im1heC5nZXJzaGZpZWxkMUBnbWFpbC5jb20iLCJlbWFpbF92ZXJpZmllZCI6dHJ1ZSwicGluX3BvbGljeSI6eyJyZWdpb25zIjpbeyJkZXNpcmVkUmVwbGljYXRpb25Db3VudCI6MSwiaWQiOiJGUkExIn0seyJkZXNpcmVkUmVwbGljYXRpb25Db3VudCI6MSwiaWQiOiJOWUMxIn1dLCJ2ZXJzaW9uIjoxfSwibWZhX2VuYWJsZWQiOmZhbHNlLCJzdGF0dXMiOiJBQ1RJVkUifSwiYXV0aGVudGljYXRpb25UeXBlIjoic2NvcGVkS2V5Iiwic2NvcGVkS2V5S2V5IjoiOWJlZDEzNDUwNzliMTU5YzE0NDMiLCJzY29wZWRLZXlTZWNyZXQiOiI2MzAzNzBkNGVhZDNkNWZkOGZiMGQ0N2FmNzhhYTZiYWNkZmM5YzY4ODNjMzEyZDc1MTYzODRhNWYzZWRmYTk0IiwiZXhwIjoxNzkxNzI2NTM3fQ.sk7jWHU6JOAlW2eF-LQrOGg-Nh84kc0-Ja8XcjQJ2Yk",
                    logger
                );
            });
            
            services.AddSingleton<TelegramBotService>(sp =>
            {
                var telegramProvider = sp.GetRequiredService<TelegramOASIS>();
                var avatarManager = AvatarManager.Instance;
                var logger = sp.GetService<ILogger<TelegramBotService>>();
                var nftService = sp.GetRequiredService<NFTService>();
                var pinataService = sp.GetRequiredService<PinataService>();
                
                // Bot token from OASIS_DNA.json TelegramOASIS config - TIMORIDES BOT
                string botToken = "8000192131:AAE3DY-AxbnhaPBaLF_mBogV169CeRXGleg";
                
                return new TelegramBotService(botToken, telegramProvider, avatarManager, logger, nftService, pinataService);
            });

            // Register Universal Asset Bridge Service
            services.AddSingleton<BridgeService>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<BridgeService>>();
                var configuration = sp.GetRequiredService<IConfiguration>();
                return new BridgeService(logger, configuration);
            });
            
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
            
            // Activate TelegramOASIS provider and start bot
            try
            {
                var telegramProvider = app.ApplicationServices.GetService<TelegramOASIS>();
                if (telegramProvider != null)
                {
                    var activationResult = telegramProvider.ActivateProvider();
                    if (activationResult.IsError)
                    {
                        LoggingManager.Log($"❌ Error activating TelegramOASIS: {activationResult.Message}", LogType.Error);
                    }
                    else
                    {
                        LoggingManager.Log("✅ TelegramOASIS provider activated successfully", LogType.Info);
                    }
                }
                
                // Start the Telegram bot service
                var botService = app.ApplicationServices.GetService<TelegramBotService>();
                if (botService != null)
                {
                    botService.StartReceiving();
                    LoggingManager.Log("✅ Telegram bot started receiving messages", LogType.Info);
                }
                else
                {
                    LoggingManager.Log("⚠️ TelegramBotService not found in DI container", LogType.Warning);
                }
            }
            catch (Exception ex)
            {
                LoggingManager.Log($"❌ Exception activating TelegramOASIS or starting bot: {ex.Message}", LogType.Error);
            }

            // migrate database changes on startup (includes initial db creation)
            //context.Database.Migrate();

            //            IApplicationBuilder app, IHostingEnvironment env)
            //{
            //                app.UseDeveloperExceptionPage();
            //                app.UseStaticFiles();
            //                app.UseMvcWithDefaultRoute();
            //            }


            // generated swagger json and swagger ui middleware
            app.UseSwagger();
            app.UseSwaggerUI(x => x.SwaggerEndpoint("/swagger/v1/swagger.json", VERSION));

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

            app.UseHttpsRedirection();

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
            app.UseMiddleware<SubscriptionMiddleware>();

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
