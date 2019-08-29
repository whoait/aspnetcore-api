using AspNetCore.API.Core;
using AspNetCore.API.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.Swagger;
using System;
using System.IO;
using System.Reflection;


namespace AspNetCore.API
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IHostingEnvironment hostingEnvironment)
        {
            Configuration = configuration;
            HostingEnvironment = hostingEnvironment;
        }

        public IConfiguration Configuration { get; }
        public IHostingEnvironment HostingEnvironment { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            GlobalConfiguration.WebRootPath = this.HostingEnvironment.WebRootPath;
            GlobalConfiguration.ContentRootPath = this.HostingEnvironment.ContentRootPath;

            //CORS
            services.AddCors(o => o.AddPolicy("Allow-All", builder =>
            {
                builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
            }));


            // MODULES
            services.LoadInstalledModules(this.HostingEnvironment.ContentRootPath);
            var modules = GlobalConfiguration.Modules;
            services.AddCustomizedMvc(modules);

            services.AddHttpClient("cloud.softech.vn", client =>
            {
                client.BaseAddress = new Uri("https://cloud.softech.vn");
                client.DefaultRequestHeaders.Add("Accept", "application/x-www-form-urlencoded");
            });


            // API VERSIONING
            // Nuget: Microsoft.AspNetCore.Mvc.Versioning
            services.AddApiVersioning(options => options.ReportApiVersions = true);

            // SWAGGER
            // Register the Swagger generator, defining 1 or more Swagger documents
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info
                {
                    Version = "v1",
                    Title = "ASP.NET Core Web API",
                    Description = "ASP.NET Core Web API",
                    TermsOfService = "None",
                    Contact = new Contact
                    {
                        Name = "Softech Corporation",
                        Email = "tungnt@softech.vn",
                        Url = "https://softech.vn"
                    },
                    License = new License
                    {
                        Name = "Use under LICX",
                        Url = "https://softech.vn"
                    }
                });

                // Set the comments path for the Swagger JSON and UI.
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);
            });

            services.AddRouting(options => options.LowercaseUrls = true);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // Swagger
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/bcm/swagger/v1/swagger.json", "ASP.NET Core Web API");
                //c.RoutePrefix = string.Empty;
            });

            app.UseMvc();
        }
    }
}
