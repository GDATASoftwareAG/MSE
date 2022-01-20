using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SampleExchangeApi.Console.Database;
using SampleExchangeApi.Console.Models;
using SampleExchangeApi.Console.SampleDownload;
using Xunit;

namespace SampleExchangeApi.Console_Test;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public class DockerFixture : IAsyncLifetime
{
    private static readonly IConfiguration Configuration = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json")
        .AddEnvironmentVariables()
        .Build();

    private IContainerService? _container;
    private readonly ISampleStorageHandler _storageHandler;
    private readonly MongoMetadataHandler _metadataHandler;
    public readonly string IpAddress;

    public DockerFixture()
    {
        _storageHandler = new SampleStorageHandler(Mock.Of<ILogger<SampleStorageHandler>>(),
            new OptionsWrapper<StorageOptions>(new StorageOptions
            {
                Path = Configuration["Storage:Path"]
            }));
        IpAddress = StartMongoDbContainer();

        var options = new MongoMetadataOptions();
        Configuration.GetSection("MongoDb").Bind(options);
        options.ConnectionString = $"mongodb://{IpAddress}:27017";
        _metadataHandler = new MongoMetadataHandler(new OptionsWrapper<MongoMetadataOptions>(options));
    }

    private async Task WriteFileWrapperAsync(string sha256, string s)
    {
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        await writer.WriteAsync(s);
        await writer.FlushAsync();
        stream.Position = 0;
        await _storageHandler.WriteAsync(sha256, stream);
    }

    private async Task CreateTestFilesAsync()
    {
        await WriteFileWrapperAsync("131f95c51cc819465fa1797f6ccacf9d494aaaff46fa3eac73ae63ffbdfd8267",
            "X5O!P%@AP[4\\PZX54(P^)7CC)7}$EICAR-STANDARD-ANTIVIRUS-TEST-FILE!$H+H*\n");
        await WriteFileWrapperAsync("cda0a81901ced9306d023500ff1c383d6b4bd8cebefa886faa2a627a796e87f",
            "X5O!P%@AP[4\\PZX54(P^)7CC)7}$EICAR-STANDARD-ANTIVIRUS-TEST-FILE!$H+H*\nDIE ZWEITE\n");
    }

    private async Task CreateTestExportSamplesAsync()
    {
        await _metadataHandler.InsertSampleAsync(
            new ExportSample
            {
                Sha256SampleSet = "131f95c51cc819465fa1797f6ccacf9d494aaaff46fa3eac73ae63ffbdfd8267:Classic",
                Sha256 = "131f95c51cc819465fa1797f6ccacf9d494aaaff46fa3eac73ae63ffbdfd8267",
                DoNotUseBefore = DateTime.Now.AddHours(-12),
                Imported = DateTime.Now.AddDays(-1),
                Platform = "DOS",
                SampleSet = "Classic",
                FamilyName = "foobar"
            });
        await _metadataHandler.InsertSampleAsync(
            new ExportSample
            {
                Sha256SampleSet = "cda0a81901ced9306d023500ff1c383d6b4bd8cebefa886faa2a627a796e87f:Classic",
                Sha256 = "cda0a81901ced9306d023500ff1c383d6b4bd8cebefa886faa2a627a796e87f",
                DoNotUseBefore = DateTime.Now.AddDays(4),
                Imported = DateTime.Now.AddDays(-1),
                Platform = "DOS",
                SampleSet = "Classic",
                FamilyName = "barfoo"
            });
        await _metadataHandler.InsertSampleAsync(
            new ExportSample
            {
                Sha256SampleSet = "52f1a61ae232c5dcba376c60d6ba2b22a34e3c39d2fd2563f2cc9cc7b2a77a2b:Example",
                Sha256 = "52f1a61ae232c5dcba376c60d6ba2b22a34e3c39d2fd2563f2cc9cc7b2a77a2b",
                DoNotUseBefore = DateTime.Now.AddHours(-12),
                Imported = DateTime.Now.AddDays(-1),
                Platform = "DOS",
                SampleSet = "Example",
                FamilyName = "thc"
            });
    }

    private string StartMongoDbContainer()
    {
        _container =
            new Builder().UseContainer()
                .UseImage("mongo:xenial")
                .ExposePort(27017, 27017)
                .Build()
                .Start();

        var containerIp = _container.GetConfiguration().NetworkSettings.IPAddress;

        Environment.SetEnvironmentVariable("MongoDb__ConnectionString", $"mongodb://{containerIp}:27017");

        Thread.Sleep(10000);

        return containerIp;
    }

    private static void StopDockerContainer(IService container)
    {
        container.Stop();
        container.Remove();
    }

    public async Task InitializeAsync()
    {
        await _metadataHandler.StartAsync();
        await CreateTestFilesAsync();
        await CreateTestExportSamplesAsync();
    }

    public Task DisposeAsync()
    {
        StopDockerContainer(_container);
        Directory.Delete(Configuration["Storage:Path"], true);
        return Task.CompletedTask;
    }
}
