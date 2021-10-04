using System;
using System.IO;
using System.Reflection;
using AutoMapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Middleware;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Services.Solana;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI
{
    public class Startup
    {
        private const string VERSION = "OASIS API v0.17.2 ALPHA";
        readonly string MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            LoggingManager.Log("Starting up The OASIS... (REST API)", Core.Enums.LogType.Info);
            LoggingManager.Log("Test Debug", Core.Enums.LogType.Debug);
            LoggingManager.Log("Test Info", Core.Enums.LogType.Info);
            LoggingManager.Log("Test Warning", Core.Enums.LogType.Warn);
            LoggingManager.Log("Test Error", Core.Enums.LogType.Error);

            // If you wish to change the logging framework from the default (NLog) then set it below (or just change in OASIS_DNA - prefered way)
            //LoggingManager.CurrentLoggingFramework = LoggingFramework.NLog;

            //services.Configure<OASISSettings>(Configuration.GetSection("OASIS")); // Replaced by OASISConfigManager in OASISMiddleware so shares same codebase to STAR ODK.
            // services.AddMvc();

            // services.AddDbContext<DataContext>();
            //services.AddCors(); //Needed twice? It is below too...
            services.AddControllers().AddJsonOptions(x => x.JsonSerializerOptions.IgnoreNullValues = true);
            services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo 
                {
                    Contact = new OpenApiContact()
                    {
                        Email = "ourworld@nextgensoftware.co.uk",
                        Name = "OASIS API"
                    },
                    Description = "The OASIS API that powers Our World and the satillite apps/games/websites (OAPP's/Moons) that plug into it. Check out <a target='_blank' href='https://drive.google.com/file/d/1nnhGpXcprr6kota1Y85HDDKsBfJHN6sn/view?usp=sharing'>The POWER Of The OASIS API</a> for more info.\n\nTo use the OASIS API follow these steps: \n\n <ol><li>First you need to create your avatar using the avatar/register method.</li><li>You will then receive an email to confirm your addrsss with a token. You then need to call the avatar/verify-email method with this token to verify and activate your new avatar.</li><li>Now you can call the avatar/authenticate method to login and authenticate. This will return your avatar object, which will contain a JWT (JSON Web Token) Security Token.</li><li>You can then set this in your HEADER for all future API calls. See descriptions below for each method for more details on how to use the OASIS API...</li></ol>\n\nYou will note that every request below has a corresponding overload that also takes a providerType. This allows you to overrite the default provider configured for the ONODE you are making the request from. The ONODE can be configured to have a list of default providers, which it will fail over to the next one if that provider goes down/is too slow, etc. It will automatically switch to the fastest provider available (and load balance between them) but if the overload is used it will override this default behaviour. Set the setGlobal flag to false if you wish to override only for that given request or to true if you wish to persist this override for all subsequent calls. The current list of providers supported are as follows (in order of priority used):\n\n<ul><li><b>MongoDBOASIS</b> - MongoDB Provider (Document/Object Database).</li><li><b>SQLLiteOASIS</b> - SQLLite Provider (Relational Database).</li><li><b>Neo4jOASIS</b> - Neo4j Provider (Graph Database).</li><li><b>HoloOASIS</b> - Holochain Provider.</li><li><b>Solana </b> - Solana Provider.</li><li><b>EthereumOASIS</b> - Ethereum Provider.</li><li><b>EOSIOOASIS</b> - EOSIO Provider.</li><li><b>TelosOASIS</b> - Telos Provider.</li><li><b>SEEDSOASIS</b> - SEEDS Provider.</li><li><b>IPFSOASIS</b> - IPFS Provider.</li></ul>\n\nSoon many more providers will be added such as ThreeFold, ActivityPub, SOLID, BlockStack & many more. Please check the github repo link below for more details... Soon HoloOASIS will be given top priority once hosting has been found for the Holochain Conductor (because the Holo Network is not yet live).\n\nThe Avatar (complete), half the Karma & half the Provider API's are currently implemented. The rest are coming soon... The SCMS (Smart Contract Management System) API's are completed but need to be refactored with some being removed so these also cannot be used currently. These are currently used for our first business use case, B.E.B (Built Enviroment Blockchain), a construction platform built on top of the OASIS API. More detailed documentation & future releases coming soon... \n\n<b>Please <a target='_blank' href='https://oasisplatform.world/OASIS_API_LIVE.postman_collection.json'>download the Postman JSON file</a> and import it into <a href='https://www.postman.com/' target='_blank'>Postman</a> if you wish to have a play/test and get famaliar with the OASIS API before plugging it into your website/app/game/service.\n\nThis project is Open Source and if you have any feedback or better still, wish to get involved we would love to hear from you, please contact us on <a target='_blank' href='https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK'>GitHub</a>, <a target='_blank' href='https://t.me/ourworldthegamechat'>Telegram</a>, <a target='_blank' href='https://discord.gg/q9gMKU6'>Discord</a> or using the <a href='mailto:ourworld@nextgensoftware.co.uk'>Contact</a> link below, we look forward to hearing from you...</b>\n\n<b>If you wish to receive FREE training on how to code and still get to help build a better world with us then please sign up at <a target='_blank' href='https://www.thejusticeleagueaccademy.icu/'>The Justice League Academy</a>. This is a superhero training platform that enables you to unleash your inner superhero and <b>FULL POTENTAL!</b> We <b>BELEIVE</b> in <b>YOU</b> and we will help you find your gift for the world...</b>\n\n<b>Check out the <a href='https://drive.google.com/file/d/1QPgnb39fsoXqcQx_YejdIhhoPbmSuTnF/view?usp=sharing' target='_blank'>DEV Plan/Roadmap</a> to see what has already been built and what is left to be built.</b>\n\n<b>Please join the <a target='_blank' href='https://t.me/oasisapihackalong'>OASIS API Weekly Hackalong Telegram Group</a> if you wish to get the latest news and developments as well as take part in weekly hackalongs where we can help you get up to speed ASAP.</b>\n\n<b>Please consider giving a <a target='_blank' href='http://www.gofundme.com/ourworldthegame'>donation</a> to help keep this vital project going... thank you.</b>\n\n\n\n<br><b><b>Want to make a difference in the world?\n\nWhat will be your legacy?\n\nReady to be a hero?</b>\n\n\n\n<br>Please come join the Our World Tribe on <a href='https://t.me/ourworldthegamechat'>Telegram</a> or <a href='https://discord.gg/q9gMKU6'>Discord.</a>, we look forward to seeing you there... :)</b><b><b>\n\nTOGETHER WE CAN CREATE A BETTER WORLD...</b></b>\n\n<br><b>Release History:</b>\n\n<a href='https://www.ourworldthegame.com/single-post/oasis-api-v0-0-1-altha-live'>v0.0.1 ALTHA</a>\n\n<br><b>Links</b>\n\n<a href='http://www.ourworldthegame.com'>http://www.ourworldthegame.com</a><br><a href='http://www.nextgensoftware.co.uk'>http://www.nextgensoftware.co.uk</a><br><a href='http://www.yoga4autism.com'>http://www.yoga4autism.com</a><br><a href='https://www.thejusticeleagueaccademy.icu/'>https://www.thejusticeleagueaccademy.icu/</a>\n\n<a href='https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK'>https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK</a><br><a href='http://www.gofundme.com/ourworldthegame'>http://www.gofundme.com/ourworldthegame</a>\n\n<a href='https://drive.google.com/file/d/1QPgnb39fsoXqcQx_YejdIhhoPbmSuTnF/view?usp=sharing'>DEV Plan/Roadmap</a><br><a href='https://drive.google.com/file/d/1nnhGpXcprr6kota1Y85HDDKsBfJHN6sn/view?usp=sharing'>The POWER Of The OASIS API</a><br>\n\n<a href='https://drive.google.com/file/d/1b_G08UTALUg4H3jPlBdElZAFvyRcVKj1/view?usp=sharing'>Join The Our World Tribe (Dev Requirements)</a><br><a href='https://drive.google.com/file/d/12pCk20iLw_uA1yIfojcP6WwvyOT4WRiO/view?usp=sharing'>The Our World Mission/Summary</a>\n\n<a href='http://www.facebook.com/ourworldthegame'>http://www.facebook.com/ourworldthegame</a><br><a href='http://www.twitter.com/ourworldthegame'>http://www.twitter.com/ourworldthegame</a><br><a href='https://www.youtube.com/channel/UC0_O4RwdY3lq1m3-K-njUxA'>https://www.youtube.com/channel/UC0_O4RwdY3lq1m3-K-njUxA</a>\n\n<a href='https://t.me/ourworldthegamechat'>https://t.me/ourworldthegamechat</a> (Telegram General Chat)<br><a href='https://t.me/ourworldthegame'>https://t.me/ourworldthegame</a> (Telegram Our World Annoucments)<br><a href='https://t.me/ourworldtechupdates'>https://t.me/ourworldtechupdates</a> (Telegram Our World Tech Updates)<br><a href='https://t.me/oasisapihackalong'>https://t.me/oasisapihackalong</a> OASIS API Weekly Hackalongs\n\n<a href='https://discord.gg/q9gMKU6'>https://discord.gg/q9gMKU6</a>",
                    Title = VERSION,
                    Version = "v1",
                });

                // Set the comments path for the Swagger JSON and UI.
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                    c.IncludeXmlComments(xmlPath);
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
            services.AddScoped<IAvatarService, AvatarService>();
            //services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<ISolanaService, SolanaService>();
            services.AddScoped<ICargoService, CargoService>();
            services.AddScoped<INftService, NftService>();

            services.AddCors(options =>
            {
                options.AddPolicy(MyAllowSpecificOrigins,
                builder =>
                {
                    builder.WithOrigins("https://localhost:44371").AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
                });
            });

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

            Program.IsDevEnviroment = env.IsDevelopment();

          //  if (env.IsDevelopment())
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
           // app.UseCors(MyAllowSpecificOrigins);

            app.UseAuthorization();

            app.UseMiddleware<OASISMiddleware>();
            app.UseMiddleware<ErrorHandlerMiddleware>();
            app.UseMiddleware<JwtMiddleware>();
            
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
