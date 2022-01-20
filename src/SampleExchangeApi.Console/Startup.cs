using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using SampleExchangeApi.Console.AuthHandler;
using SampleExchangeApi.Console.Database;
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
        services.AddSingleton<ISampleMetadataHandler, MongoMetadataHandler>();
        services.AddHostedService<MongoMetadataHandler>();

        services.Configure<StorageOptions>(_configuration.GetSection("Storage"));
        services.AddTransient<ISampleStorageHandler, SampleStorageHandler>();

        services.Configure<PartnerProviderOptions>(_configuration.GetSection("Config"));
        services.AddHttpClient<PartnerProvider>();
        services.AddTransient<IPartnerProvider, PartnerProvider>();

        services.Configure<ListRequesterOptions>(_configuration.GetSection("Token"));
        services.AddTransient<IListRequester, ListRequester.ListRequester>();

        services.Configure<UploadOptions>(_configuration.GetSection("Upload"));

        services
            .AddMvc(options => options.EnableEndpointRouting = false)
            .AddNewtonsoftJson(opts =>
            {
                opts.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                var item = new StringEnumConverter { NamingStrategy = new CamelCaseNamingStrategy() };
                opts.SerializerSettings.Converters.Add(item);
            });

        services.AddAuthentication("BasicAuthentication")
            .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>("BasicAuthentication", null);

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
            c.AddSecurityDefinition("basic", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "basic",
                In = ParameterLocation.Header,
                Description = "Basic Authorization header using the Bearer scheme.",
            });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "basic"
                        }
                    },
                    new string[] {}
                }
            });
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

        app.UseAuthentication();
        app.UseAuthorization();
        app.UseEndpoints(endpoints => { endpoints.MapControllers(); });

        if (_hostingEnv.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
    }
}
