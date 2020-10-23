using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using SampleExchangeApi.Console.Filters;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SampleExchangeApi.Console
{
    public class Startup
    {
        private readonly IHostingEnvironment _hostingEnv;

        public Startup(IHostingEnvironment env)
        {
            _hostingEnv = env;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddMvc()
                .AddNewtonsoftJson(opts =>
                {
                    opts.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                    var item = new StringEnumConverter {NamingStrategy = new CamelCaseNamingStrategy()};
                    opts.SerializerSettings.Converters.Add(item);
                });

            services
                .AddSwaggerGen(c =>
                {
                    c.SwaggerDoc("1.1.0", new Info
                    {
                        Version = "1.1.0",
                        Title = "G DATA Sample Exchange",
                        Description =
                            "All inquiries about the sample and URL exchange with G DATA Software AG should be sent " +
                            "to our e-mail address for Sample Exchange provided in the documentation.",
                        Contact = new Contact()
                        {
                            Name = "G DATA Software AG",
                            Url = "https://www.gdata.de/"
                        },
                        TermsOfService = ""
                    });
                    c.CustomSchemaIds(type => type.FriendlyId(true));
                    c.DescribeAllEnumsAsStrings();
                    c.IncludeXmlComments(
                        $"{AppContext.BaseDirectory}{Path.DirectorySeparatorChar}{_hostingEnv.ApplicationName}.xml");
                    // Sets the basePath property in the Swagger document generated
                    c.DocumentFilter<BasePathFilter>("/v1");

                    // Include DataAnnotation attributes on Controller Action parameters as Swagger validation rules (e.g required, pattern, ..)
                    // Use [ValidateModelState] on Actions to actually validate it in C# as well!
                    c.OperationFilter<GeneratePathParamsValidationFilter>();
                });
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseMvc()
                .UseDefaultFiles()
                .UseStaticFiles()
                .UseSwagger(c => { c.RouteTemplate = "swagger/{documentName}/openapi.json"; })
                .UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("1.1.0/openapi.json", "Sample list tool.");
                    c.SupportedSubmitMethods(new SubmitMethod[] { });
                });

            if (_hostingEnv.IsDevelopment())
                app.UseDeveloperExceptionPage();
        }
    }
}