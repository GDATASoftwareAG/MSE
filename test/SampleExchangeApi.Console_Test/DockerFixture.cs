using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Services;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using SampleExchangeApi.Console.Models;
using Xunit;

namespace SampleExchangeApi.Console_Test;

[CollectionDefinition("DockerContainerCollection")]
public class DatabaseCollection : ICollectionFixture<DockerFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public class DockerFixture : IDisposable
{
    private static readonly IConfiguration Configuration = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json")
        .AddEnvironmentVariables()
        .Build();

    private IContainerService? _container;
    public readonly string IpAddress;

    public DockerFixture()
    {
        IpAddress = StartMongoDbContainer();
    }

    private static void CreateTestFile()
    {
        try
        {
            Directory.CreateDirectory($"{Configuration["Storage:Path"]}/13/1f");
            Directory.CreateDirectory($"{Configuration["Storage:Path"]}/cd/a0");
        }
        catch (Exception)
        {
            // ignored
        }

        var eicar = "X5O!P%@AP[4\\PZX54(P^)7CC)7}$EICAR-STANDARD-ANTIVIRUS-TEST-FILE!$H+H*\n";
        var eicarZwei = "X5O!P%@AP[4\\PZX54(P^)7CC)7}$EICAR-STANDARD-ANTIVIRUS-TEST-FILE!$H+H*\nDIE ZWEITE\n";

        using (var file =
               File.Create(
                   $"{Configuration["Storage:Path"]}/13/1f/131f95c51cc819465fa1797f6ccacf9d494aaaff46fa3eac73ae63ffbdfd8267")
              )
        {
            file.Write(Encoding.ASCII.GetBytes(eicar), 0, eicar.Length);
        }

        using (var file =
               File.Create(
                   $"{Configuration["Storage:Path"]}/cd/a0/cda0a81901ced9306d023500ff1c383d6b4bd8cebefa886faa2a627a796e87f")
              )
        {
            file.Write(Encoding.ASCII.GetBytes(eicarZwei), 0, eicar.Length);
        }
    }

    private static void WriteFakeDataIntoTestMongo(IMongoClient mongoClient)
    {
        var mongoDatabase = mongoClient.GetDatabase(Configuration["MongoDb:DatabaseName"]);
        var mongoCollection = mongoDatabase.GetCollection<ExportSample>(Configuration["MongoDb:CollectionName"]);

        var indexes = Configuration.GetSection("MongoDb:Indexes").GetChildren().ToArray().Select(c => c.Value)
            .ToArray();

        foreach (var index in indexes)
        {
            mongoCollection.Indexes.CreateOne(
                new CreateIndexModel<ExportSample>(Builders<ExportSample>.IndexKeys.Ascending(index)));
        }

        mongoCollection.InsertOne(
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
        mongoCollection.InsertOne(
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
        mongoCollection.InsertOne(
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

        var mongoClient = new MongoClient($"mongodb://{containerIp}:27017");
        WriteFakeDataIntoTestMongo(mongoClient);
        CreateTestFile();

        return containerIp;
    }

    private static void StopDockerContainer(IService container)
    {
        container.Stop();
        container.Remove();
    }

    public void Dispose()
    {
        StopDockerContainer(_container);
    }
}
