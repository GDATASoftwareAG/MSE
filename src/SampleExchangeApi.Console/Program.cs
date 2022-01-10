using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SampleExchangeApi.Console;
using SampleExchangeApi.Console.Database;
using SampleExchangeApi.Console.Database.TempSampleDB;
using SampleExchangeApi.Console.ListRequester;
using SampleExchangeApi.Console.SampleDownload;

await Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(builder =>
    {
        builder.AddEnvironmentVariables();
        builder.AddCommandLine(args);
    })
    .ConfigureLogging(logging =>
    {
        logging.AddJsonConsole();
    })
    .ConfigureWebHostDefaults(builder =>
    {
        builder.UseStartup<Startup>();
        builder.UseKestrel();
    })
    .ConfigureServices((context, services) =>
    {
        services.Configure<MongoMetadataOptions>(context.Configuration.GetSection("MongoDb"));
        services.AddTransient<ISampleMetadataReader, MongoMetadataReader>();

        services.Configure<StorageOptions>(context.Configuration.GetSection("Storage"));
        services.AddTransient<ISampleGetter, SampleGetter>();

        services.Configure<PartnerProviderOptions>(context.Configuration.GetSection("Config"));
        services.AddTransient<IPartnerProvider, PartnerProvider>();

        services.Configure<ListRequesterOptions>(context.Configuration.GetSection("Token"));
        services.AddTransient<IListRequester, ListRequester>();
    })
    .RunConsoleAsync();
