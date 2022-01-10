using System;
using System.IO;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using SampleExchangeApi.Console.Database.TempSampleDB;
using SampleExchangeApi.Console.ListRequester;
using SampleExchangeApi.Console.Models;
using SampleExchangeApi.Console.SampleDownload;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SampleExchangeApi.Console;

public static class Program
{
    private static readonly IConfiguration Config = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json")
        .AddEnvironmentVariables()
        .Build();

    private static readonly ILogger Logger = LoggerFactory.Create(builder =>
    {
        builder.AddConsole();
        builder.AddFilter("Logger", LogLevel.Information);
    }).CreateLogger("Logger");

    public static void Main(string[] args)
    {
        Logger.LogInformation("Service Start.");

        WebHost.CreateDefaultBuilder(args)
            .UseStartup<Startup>()
            .ConfigureServices(services =>
            {
                services.AddSingleton(GetShareConfig());
                services.AddSingleton<IMongoClient>(new MongoClient(Config["MongoDb:ConnectionString"]));
                services.AddSingleton(Logger);
                services.AddTransient<IListRequester, ListRequester.ListRequester>();
                services.AddTransient<ISampleGetter, SampleGetter>();
                services.AddTransient<ISampleMetadataReader, MongoMetadataReader>();
            })
            .UseUrls(Config["Communication:REST:URL"])
            .Build()
            .Run();
    }

    private static Settings GetShareConfig()
    {
        try
        {
            var document = File.ReadAllText(Config["Config:YAML"]);
            var input = new StringReader(document);

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(PascalCaseNamingConvention.Instance)
                .Build();

            return deserializer.Deserialize<Settings>(input);
        }
        catch (Exception e)
        {
            Logger.LogError(e.ToString());
            Environment.Exit(1);
            return null;
        }
    }
}
