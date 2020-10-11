using System;
using AutoMapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Middleware;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services;
using NextGenSoftware.OASIS.API.WebAPI;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI
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
            services.Configure<OASISSettings>(Configuration.GetSection("OASIS"));

           // services.AddDbContext<DataContext>();
            //services.AddCors(); //Needed twice? It is below too...
            services.AddControllers().AddJsonOptions(x => x.JsonSerializerOptions.IgnoreNullValues = true);
            services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
            //services.AddSwaggerGen();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Contact = new OpenApiContact() { Email = "ourworld@nextgensoftware.co.uk", Name = "OASIS API", Url = new Uri("https://github.com/NextGenSoftwareUK/Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK") }, Description = "The OASIS API that powers Our World and the satillite apps/games/websites (OAPP's/Moons) that plug into it. Documentation coming soon...", Title = "OASIS API v0.01 ALTHA", Version = "v1", });
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
            services.AddScoped<IEmailService, EmailService>();

            services.AddCors(options =>
            {
                options.AddPolicy(MyAllowSpecificOrigins,
                builder =>
                {
                    builder.WithOrigins("https://localhost:44371").AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
                });
            });

            services.AddControllers();

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

            // generated swagger json and swagger ui middleware
            app.UseSwagger();
            app.UseSwaggerUI(x => x.SwaggerEndpoint("/swagger/v1/swagger.json", "OASIS API v0.01 ALTHA"));

            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();

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
