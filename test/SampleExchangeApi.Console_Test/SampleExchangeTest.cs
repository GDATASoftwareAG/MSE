using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using JWT.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Moq;
using SampleExchangeApi.Console.Database.TempSampleDB;
using SampleExchangeApi.Console.ListRequester;
using SampleExchangeApi.Console.Models;
using SampleExchangeApi.Console.SampleDownload;
using Xunit;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using JWT.Algorithms;
using Microsoft.Extensions.Options;
using SampleExchangeApi.Console.Database;

namespace SampleExchangeApi.Console_Test;

[Collection("DockerContainerCollection")]
public class SampleExchangeTest
{
    private readonly DockerFixture _dockerFixture;

    private static readonly IConfiguration Configuration = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json")
        .AddEnvironmentVariables()
        .Build();

    public SampleExchangeTest(DockerFixture dockerFixture)
    {
        _dockerFixture = dockerFixture;
    }

    private static void CreateTestFile()
    {
        try
        {
            Directory.CreateDirectory($"{Configuration["Storage:Path"]}/c7/9a");
            Directory.CreateDirectory($"{Configuration["Storage:Path"]}/58/8b");
        }
        catch (Exception)
        {
            // ignored
        }

        var fileContent1 = "lfhaerlghseargherghserligesrg";
        var fileContent2 = "reglehrger45u9pewrfgadlkjgfsfdfsdf234";

        using (var file =
               File.Create(
                   $"{Configuration["Storage:Path"]}/c7/9a/c79a962e9dc9f4251fd2bf4398d4676b36ed8814c46c0807bf68f466652b35d0")
              )
        {
            file.Write(Encoding.ASCII.GetBytes(fileContent1), 0, fileContent1.Length);
        }

        using (var file =
               File.Create(
                   $"{Configuration["Storage:Path"]}/58/8b/588b719918e06e13a73744dff033ff77e4c076f6c8f0733ce453549aed518aa4")
              )
        {
            file.Write(Encoding.ASCII.GetBytes(fileContent2), 0, fileContent2.Length);
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
                Sha256SampleSet = "c79a962e9dc9f4251fd2bf4398d4676b36ed8814c46c0807bf68f466652b35d0:Classic",
                Sha256 = "c79a962e9dc9f4251fd2bf4398d4676b36ed8814c46c0807bf68f466652b35d0",
                DoNotUseBefore = DateTime.Now.AddHours(-12),
                Imported = DateTime.Now.AddDays(-1),
                Platform = "DOS",
                SampleSet = "Classic"
            });
        mongoCollection.InsertOne(
            new ExportSample
            {
                Sha256SampleSet = "588b719918e06e13a73744dff033ff77e4c076f6c8f0733ce453549aed518aa4:Classic",
                Sha256 = "588b719918e06e13a73744dff033ff77e4c076f6c8f0733ce453549aed518aa4",
                DoNotUseBefore = DateTime.Now.AddDays(4),
                Imported = DateTime.Now.AddDays(-1),
                Platform = "DOS",
                SampleSet = "Classic"
            });
        mongoCollection.InsertOne(
            new ExportSample
            {
                Sha256SampleSet = "52f1a61ae232c5dcba376c60d6ba2b22a34e3c39d2fd2563f2cc9cc7b2a77a2b:Example",
                Sha256 = "52f1a61ae232c5dcba376c60d6ba2b22a34e3c39d2fd2563f2cc9cc7b2a77a2b",
                DoNotUseBefore = DateTime.Now.AddHours(-12),
                Imported = DateTime.Now.AddDays(-1),
                Platform = "DOS",
                SampleSet = "Example"
            });
    }

    private static PartnerProvider CreatePartnerProvider()
    {
        return new PartnerProvider(Mock.Of<ILogger<PartnerProvider>>(),
            new OptionsWrapper<PartnerProviderOptions>(new PartnerProviderOptions
            {
                YAML = Configuration["Config:YAML"]
            }));
    }
    private ISampleGetter CreateSampleGetter()
    {
        var options = new StorageOptions();
        Configuration.GetSection("Storage").Bind(options);

        var sampleGetter = new SampleGetter(Mock.Of<ILogger<SampleGetter>>(), new OptionsWrapper<StorageOptions>(options));
        return sampleGetter;
    }

    private ListRequester CreateListRequester(ISampleMetadataReader? sampleMetadataReader = null)
    {
        sampleMetadataReader ??= Mock.Of<ISampleMetadataReader>();
        var options = new ListRequesterOptions();
        Configuration.GetSection("Token").Bind(options);
        return new ListRequester(Mock.Of<ILogger<ListRequester>>(), new OptionsWrapper<ListRequesterOptions>(options), sampleMetadataReader,
            CreatePartnerProvider(), CreateSampleGetter());
    }
    private MongoMetadataReader CreateMongoMetadataReader()
    {
        var options = new MongoMetadataOptions();
        Configuration.GetSection("MongoDb").Bind(options);
        options.ConnectionString = $"mongodb://{_dockerFixture.IpAddress}:27017";
        return new MongoMetadataReader(Mock.Of<ILogger<MongoMetadataReader>>(), new OptionsWrapper<MongoMetadataOptions>(options));
    }

    [Fact]
    public void AreCredentialsOkay_PartnerDoesNotExist_ReturnsFalse()
    {
        var listRequester = CreateListRequester();

        Assert.False(listRequester
            .AreCredentialsOkay("ThisPartnerDoesNotExist", "FalschesPasswort", "eltesto"));
    }

    [Fact]
    public void AreCredentialsOkay_PartnerDoesExistButWrongPassword_ReturnsFalse()
    {
        var listRequester = CreateListRequester();

        Assert.False(listRequester
            .AreCredentialsOkay("netisee", "FalschesPasswort", "eltesto"));
    }

    [Fact]
    public void AreCredentialsOkay_CredentialsAreOk_ReturnsTrue()
    {
        var listRequester = CreateListRequester();

        Assert.True(listRequester
            .AreCredentialsOkay("partner2", "test123", "eltesto"));
    }

    [Fact]
    public async void BusinessLogicCallback_GetSampleToken()
    {
        string sha256String;
        var mongoClient = new MongoClient($"mongodb://{_dockerFixture.IpAddress}:27017");
        var reader = CreateMongoMetadataReader();
        var sampleGetter = CreateSampleGetter();
        var listRequester = CreateListRequester(reader);


        WriteFakeDataIntoTestMongo(mongoClient);
        CreateTestFile();

        var tokens = await listRequester
            .RequestListAsync("partner2", DateTime.Now.AddDays(-7),
                null, "eltesto");

        var deserializedToken = new JwtBuilder()
            .WithAlgorithm(new HMACSHA512Algorithm())
            .WithSecret(Configuration["Token:Secret"])
            .MustVerifySignature()
            .Decode<IDictionary<string, object>>(tokens[0]._Token);

        var sha256FromToken = deserializedToken["sha256"].ToString();
        var partnerFromToken = deserializedToken["partner"].ToString();
        var filesizeFromToken = long.Parse(deserializedToken["filesize"].ToString());

        using (var sha256 = SHA256.Create())
        {
            sha256String = Convert.ToHexString(sha256
                .ComputeHash(sampleGetter
                    .Get(sha256FromToken, partnerFromToken).FileStream)).ToLower();
        }

        Assert.Equal("c79a962e9dc9f4251fd2bf4398d4676b36ed8814c46c0807bf68f466652b35d0", sha256String);
        Assert.Equal(29, filesizeFromToken);
    }

}
