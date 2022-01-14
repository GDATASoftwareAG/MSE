using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using SampleExchangeApi.Console.Database;
using SampleExchangeApi.Console.Database.TempSampleDB;
using SampleExchangeApi.Console.ListRequester;
using SampleExchangeApi.Console.SampleDownload;

namespace SampleExchangeApi.Console;

public class Startup
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _hostingEnv;

    public Startup(IConfiguration configuration, IWebHostEnvironment env)
    {
        _configuration = configuration;
        _hostingEnv = env;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.Configure<MongoMetadataOptions>(_configuration.GetSection("MongoDb"));
        services.AddTransient<ISampleMetadataHandler, MongoMetadataHandler>();

        services.Configure<StorageOptions>(_configuration.GetSection("Storage"));
        services.AddTransient<ISampleStorageHandler, SampleStorageHandler>();

        services.Configure<PartnerProviderOptions>(_configuration.GetSection("Config"));
        services.AddTransient<IPartnerProvider, PartnerProvider>();

        services.Configure<ListRequesterOptions>(_configuration.GetSection("Token"));
        services.AddTransient<IListRequester, ListRequester.ListRequester>();

        services
            .AddMvc(options => options.EnableEndpointRouting = false)
            .AddNewtonsoftJson(opts =>
            {
                opts.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                var item = new StringEnumConverter { NamingStrategy = new CamelCaseNamingStrategy() };
                opts.SerializerSettings.Converters.Add(item);
            });

        // Add OpenAPI generator
        services.AddSwaggerGen(c =>
        {
            c.EnableAnnotations();
            c.SwaggerDoc("v1",
                new OpenApiInfo
                {
                    Title = "Malware Sample Exchange - V1",
                    Version = "v1",
                    Description = "https://github.com/GDATASoftwareAG/MSE"
                }
            );
        });
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseDefaultFiles()
            .UseForwardedHeaders()
            .UseRouting()
            .UseStaticFiles()
            .UseSwagger()
            .UseSwaggerUI(options =>
            {
                options.DocumentTitle = "Malware Sample Exchange - V1";
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Malware Sample Exchange - V1");
            });

        app.UseEndpoints(endpoints => { endpoints.MapControllers(); });

        if (_hostingEnv.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
    }
}
