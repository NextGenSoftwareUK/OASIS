using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.ONODE.WebUI.HTML2
{
    public class Startup
    {
        readonly string MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages();

            // services.AddDbContext<DataContext>();
            //services.AddCors(); //Needed twice? It is below too...
            services.AddControllers().AddJsonOptions(x => x.JsonSerializerOptions.IgnoreNullValues = true);
           // services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

            /*
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    //Contact = new OpenApiContact() 
                    //{
                    //    Email = "ourworld@nextgensoftware.co.uk", 
                    //    Name = "OASIS API", 
                    //    Url = new Uri("https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK") }, 
                    //    Description = "The OASIS API that powers Our World and the satillite apps/games/websites (OApp's/Moons) that plug into it.\n\nTo use the OASIS API follow these steps: \n\n <ol><li>First you need to create your avatar using the avatar/register method.</li><li>You will then receive an email to confirm your addrsss with a token. You then need to call the avatar/verify-email method with this token to verify and activate your new avatar.</li><li>Now you can call the avatar/authenticate method to login and authenticate. This will return your avatar object, which will contain a JWT (JSON Web Token) Security Token.</li><li>You can then set this in your HEADER for all future API calls. See descriptions below for each method for more details on how to use the OASIS API...</li></ol>\n\nYou will note that every request below has a corresponding overload that also takes a providerType. This allows you to overrite the default provider configured for the ONODE you are making the request from. The ONODE can be configured to have a list of default providers, which it will fail over to the next one if that provider goes down/is too slow, etc. It will automatically switch to the fastest provider available (and load balance between them) but if the overload is used it will override this default behaviour. Set the setGlobal flag to false if you wish to override only for that given request or to true if you wish to persist this override for all subsequent calls. The current list of providers supported are as follows (in order of priority used):\n\n<ul><li><b>MongoDBOASIS</b> - MongoDB Provider.</li><li><b>HoloOASIS</b> - Holochain Provider.</li><li><b>EthereumOASIS</b> - Ethereum Provider.</li><li><b>EOSIOOASIS</b> - EOSIO Provider.</li><li><b>SQLLiteDBOASIS</b> - SQLLite Provider.</li></ul>\n\nSoon many more providers will be added such as ThreeFold, ActivityPub, IPFS, SOLID, BlockStack & many more. Please check the github repo link below for more details... Soon HoloOASIS will be given top priority once hosting has been found for the Holochain Conductor (because the Holo Network is not yet live).\n\nThe Avatar (complete), half the Karma & half the Provider API's are currently implemented. The rest are coming soon... The SCMS (Smart Contract Management System) API's are completed but need to be refactored with some being removed so these also cannot be used currently. These are currently used for our first business use case, B.E.B (Built Enviroment Blockchain), a construction platform built on top of the OASIS API. More detailed documentation & future releases coming soon...\n\n<b>TOGETHER WE CAN CREATE A BETTER WORLD...<b>\n\n<a href='http://www.ourworldthegame.com'>http://www.ourworldthegame.com</a>\n\n<a href='https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK'>Github Repo</a>", 
                    //    Title = "OASIS API v0.01 ALTHA",
                    //    Version = "v1", 
                    //});

                    Contact = new OpenApiContact()
                    {
                        Email = "ourworld@nextgensoftware.co.uk",
                        Name = "OASIS API"
                    },
                    //Description = "The OASIS API that powers Our World and the satillite apps/games/websites (OApp's/Moons) that plug into it.<br>To use the OASIS API follow these steps: <br><ol><li>First you need to create your avatar using the avatar/register method.</li><li>You will then receive an email to confirm your addrsss with a token. You then need to call the avatar/verify-email method with this token to verify and activate your new avatar.</li><li>Now you can call the avatar/authenticate method to login and authenticate. This will return your avatar object, which will contain a JWT (JSON Web Token) Security Token.</li><li>You can then set this in your HEADER for all future API calls. See descriptions below for each method for more details on how to use the OASIS API...</li></ol><br><br>You will note that every request below has a corresponding overload that also takes a providerType. This allows you to overrite the default provider configured for the ONODE you are making the request from. The ONODE can be configured to have a list of default providers, which it will fail over to the next one if that provider goes down/is too slow, etc. It will automatically switch to the fastest provider available (and load balance between them) but if the overload is used it will override this default behaviour. Set the setGlobal flag to false if you wish to override only for that given request or to true if you wish to persist this override for all subsequent calls. The current list of providers supported are as follows (in order of priority used):<br><ul><li><b>MongoDBOASIS</b> - MongoDB Provider.</li><li><b>HoloOASIS</b> - Holochain Provider.</li><li><b>EthereumOASIS</b> - Ethereum Provider.</li><li><b>EOSIOOASIS</b> - EOSIO Provider.</li><li><b>SQLLiteDBOASIS</b> - SQLLite Provider.</li></ul><br>Soon many more providers will be added such as ThreeFold, ActivityPub, IPFS, SOLID, BlockStack & many more. Please check the github repo link below for more details... Soon HoloOASIS will be given top priority once hosting has been found for the Holochain Conductor (because the Holo Network is not yet live).<br>The Avatar (complete), half the Karma & half the Provider API's are currently implemented. The rest are coming soon... The SCMS (Smart Contract Management System) API's are completed but need to be refactored with some being removed so these also cannot be used currently. These are currently used for our first business use case, B.E.B (Built Enviroment Blockchain), a construction platform built on top of the OASIS API. More detailed documentation & future releases coming soon... <br><br><b>This project is Open Source and if you have any feedback or better still, wish to get involved we would love to hear from you, please contact us on <a href='https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK'>GitHub</a> or using the <a href='mailto:ourworld@nextgensoftware.co.uk'>Contact</a> link below, we look forward to hearing from you...</b><br><b>Please consider giving a <a href='http://www.gofundme.com/ourworldthegame'>donation</a> to help keep this vital project going... thank you.</b><br><b>Want to make a difference in the world? What will be your legacy? Ready to be a hero? Please come join the <a href='https://t.me/ourworldthegamechat'>Our World Tribe</a>...</b><b><bTOGETHER WE CAN CREATE A BETTER WORLD...</b><br><b>Release History:</b><br><a href='https://www.ourworldthegame.com/single-post/oasis-api-v0-0-1-altha-live'>v0.0.1 ALTHA</a><br><a href='http://www.ourworldthegame.com'>http://www.ourworldthegame.com</a><br><a href='https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK'>https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK</a><br><a href='http://www.gofundme.com/ourworldthegame'>http://www.gofundme.com/ourworldthegame</a>",
                    Description = "The OASIS API that powers Our World and the satillite apps/games/websites (OApp's/Moons) that plug into it.\n\nTo use the OASIS API follow these steps: \n\n <ol><li>First you need to create your avatar using the avatar/register method.</li><li>You will then receive an email to confirm your addrsss with a token. You then need to call the avatar/verify-email method with this token to verify and activate your new avatar.</li><li>Now you can call the avatar/authenticate method to login and authenticate. This will return your avatar object, which will contain a JWT (JSON Web Token) Security Token.</li><li>You can then set this in your HEADER for all future API calls. See descriptions below for each method for more details on how to use the OASIS API...</li></ol>\n\nYou will note that every request below has a corresponding overload that also takes a providerType. This allows you to overrite the default provider configured for the ONODE you are making the request from. The ONODE can be configured to have a list of default providers, which it will fail over to the next one if that provider goes down/is too slow, etc. It will automatically switch to the fastest provider available (and load balance between them) but if the overload is used it will override this default behaviour. Set the setGlobal flag to false if you wish to override only for that given request or to true if you wish to persist this override for all subsequent calls. The current list of providers supported are as follows (in order of priority used):\n\n<ul><li><b>MongoDBOASIS</b> - MongoDB Provider (Document/Object Database).</li><li><b>SQLLiteDBOASIS</b> - SQLLite Provider (Relational Database).</li><li><b>Neo4jOASIS</b> - Neo4j Provider (Graph Database).</li><li><b>HoloOASIS</b> - Holochain Provider.</li><li><b>EthereumOASIS</b> - Ethereum Provider.</li><li><b>EOSIOOASIS</b> - EOSIO Provider.</li><li><b>IPFSOASIS</b> - IPFS Provider.</li></ul>\n\nSoon many more providers will be added such as ThreeFold, ActivityPub, SOLID, BlockStack & many more. Please check the github repo link below for more details... Soon HoloOASIS will be given top priority once hosting has been found for the Holochain Conductor (because the Holo Network is not yet live).\n\nThe Avatar (complete), half the Karma & half the Provider API's are currently implemented. The rest are coming soon... The SCMS (Smart Contract Management System) API's are completed but need to be refactored with some being removed so these also cannot be used currently. These are currently used for our first business use case, B.E.B (Built Enviroment Blockchain), a construction platform built on top of the OASIS API. More detailed documentation & future releases coming soon... \n\n<b>This project is Open Source and if you have any feedback or better still, wish to get involved we would love to hear from you, please contact us on <a href='https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK'>GitHub</a>, <a href='https://t.me/ourworldthegamechat'>Telegram</a>, <a href='https://discord.gg/q9gMKU6'>Discord</a> or using the <a href='mailto:ourworld@nextgensoftware.co.uk'>Contact</a> link below, we look forward to hearing from you...</b>\n\n<b>Please consider giving a <a href='http://www.gofundme.com/ourworldthegame'>donation</a> to help keep this vital project going... thank you.</b>\n\n<b>Want to make a difference in the world? What will be your legacy? Ready to be a hero? Please come join the Our World Tribe on <a href='https://t.me/ourworldthegamechat'>Telegram</a> or <a href='https://discord.gg/q9gMKU6'>Discord.</a>, we look forward to seeing you there... :)</b><b>\n\nTOGETHER WE CAN CREATE A BETTER WORLD...</b>\n\n<b>Release History:</b>\n\n<a href='https://www.ourworldthegame.com/single-post/oasis-api-v0-0-1-altha-live'>v0.0.1 ALTHA</a>\n\n<b>Links</b>\n\n<a href='http://www.ourworldthegame.com'>http://www.ourworldthegame.com</a><br><a href='http://www.nextgensoftware.co.uk'>http://www.nextgensoftware.co.uk</a><br><a href='http://www.yoga4autism.com'>http://www.yoga4autism.com</a>\n\n<a href='https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK'>https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK</a><br><a href='http://www.gofundme.com/ourworldthegame'>http://www.gofundme.com/ourworldthegame</a>\n\n<a href='https://drive.google.com/file/d/1b_G08UTALUg4H3jPlBdElZAFvyRcVKj1/view?usp=sharing'>Join The Our World Tribe (Dev Requirements)</a><br><a href='https://drive.google.com/file/d/12pCk20iLw_uA1yIfojcP6WwvyOT4WRiO/view?usp=sharing'>The Our World Mission/Summary</a>\n\n<a href='http://www.facebook.com/ourworldthegame'>http://www.facebook.com/ourworldthegame</a><br><a href='http://www.twitter.com/ourworldthegame'>http://www.twitter.com/ourworldthegame</a><br><a href='https://www.youtube.com/channel/UC0_O4RwdY3lq1m3-K-njUxA'>https://www.youtube.com/channel/UC0_O4RwdY3lq1m3-K-njUxA</a>\n\n<a href='https://t.me/ourworldthegamechat'>https://t.me/ourworldthegamechat</a> (Telegram General Chat)<br><a href='https://t.me/ourworldthegame'>https://t.me/ourworldthegame</a> (Telegram Our World Annoucments)<br><a href='https://t.me/ourworldtechupdates'>https://t.me/ourworldtechupdates</a> (Telegram Our World Tech Updates)\n\n<a href='https://discord.gg/q9gMKU6'>https://discord.gg/q9gMKU6</a>",
                    Title = VERSION,
                    Version = "v1",
                });

                // Set the comments path for the Swagger JSON and UI.
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);
            });*/


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
           // services.AddScoped<IAvatarService, AvatarService>();
           // services.AddScoped<IEmailService, EmailService>();

            services.AddCors(options =>
            {
                options.AddPolicy(MyAllowSpecificOrigins,
                builder =>
                {
                    builder.WithOrigins("https://localhost:44371").AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
                });
            });

           // services.AddControllers();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapControllers();
            });


           // app.UseSwagger();
           // app.UseSwaggerUI(x => x.SwaggerEndpoint("/swagger/v1/swagger.json", VERSION));

           // Program.IsDevEnviroment = env.IsDevelopment();

            //  if (env.IsDevelopment())
        //    app.UseDeveloperExceptionPage();
    

            // global cors policy
            app.UseCors(x => x
                .SetIsOriginAllowed(origin => true)
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials());

            //TODO: Was this, check later...
            // app.UseCors(MyAllowSpecificOrigins);

   

            //app.UseMiddleware<OASISMiddleware>();
           // app.UseMiddleware<ErrorHandlerMiddleware>();
           // app.UseMiddleware<JwtMiddleware>();

        }
    }
}
