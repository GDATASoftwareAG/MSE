using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace SampleExchangeApi.Console
{
    public class Startup
    {
        private readonly IWebHostEnvironment _hostingEnv;

        public Startup(IWebHostEnvironment env)
        {
            _hostingEnv = env;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddMvc(options => options.EnableEndpointRouting = false)
                .AddNewtonsoftJson(opts =>
                {
                    opts.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                    var item = new StringEnumConverter {NamingStrategy = new CamelCaseNamingStrategy()};
                    opts.SerializerSettings.Converters.Add(item);
                });
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseMvc()
                .UseDefaultFiles()
                .UseStaticFiles();

            if (_hostingEnv.IsDevelopment())
                app.UseDeveloperExceptionPage();
        }
    }
}