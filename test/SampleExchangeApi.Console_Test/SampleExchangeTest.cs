using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
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

        var sampleGetter =
            new SampleGetter(Mock.Of<ILogger<SampleGetter>>(), new OptionsWrapper<StorageOptions>(options));
        return sampleGetter;
    }

    private ListRequester CreateListRequester(ISampleMetadataReader? sampleMetadataReader = null)
    {
        sampleMetadataReader ??= Mock.Of<ISampleMetadataReader>();
        var options = new ListRequesterOptions();
        Configuration.GetSection("Token").Bind(options);
        return new ListRequester(Mock.Of<ILogger<ListRequester>>(), new OptionsWrapper<ListRequesterOptions>(options),
            sampleMetadataReader,
            CreatePartnerProvider(), CreateSampleGetter());
    }

    private MongoMetadataReader CreateMongoMetadataReader()
    {
        var options = new MongoMetadataOptions();
        Configuration.GetSection("MongoDb").Bind(options);
        options.ConnectionString = $"mongodb://{_dockerFixture.IpAddress}:27017";
        return new MongoMetadataReader(Mock.Of<ILogger<MongoMetadataReader>>(),
            new OptionsWrapper<MongoMetadataOptions>(options));
    }

    private static string HexStringFromBytes(IEnumerable<byte> bytes)
    {
        var sb = new StringBuilder();
        foreach (var b in bytes)
        {
            var hex = b.ToString("x2");
            sb.Append(hex);
        }

        return sb.ToString();
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
    public async void BusinessLogicCallback_GetSampleToken_NoFamilyName()
    {
        string sha256String;
        var reader = CreateMongoMetadataReader();
        var sampleGetter = CreateSampleGetter();
        var listRequester = CreateListRequester(reader);

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
            sha256String = HexStringFromBytes(sha256
                .ComputeHash(sampleGetter.GetAsync(sha256FromToken, partnerFromToken).GetAwaiter().GetResult().FileStream));
        }

        Assert.Single(tokens);
        Assert.False(deserializedToken.ContainsKey("familyname"));
        Assert.Equal("131f95c51cc819465fa1797f6ccacf9d494aaaff46fa3eac73ae63ffbdfd8267", sha256String);
        Assert.Equal(69, filesizeFromToken);
    }

    [Fact]
    public async void BusinessLogicCallback_GetSampleToken_HasFamilyName()
    {
        string sha256String;
        var jwtBuilder = new JwtBuilder()
            .WithAlgorithm(new HMACSHA512Algorithm())
            .WithSecret(Configuration["Token:Secret"])
            .MustVerifySignature();
        var reader = CreateMongoMetadataReader();
        var sampleGetter = CreateSampleGetter();
        var listRequester = CreateListRequester(reader);

        var tokens = await listRequester
            .RequestListAsync("partnerWithFamilyName", DateTime.Now.AddDays(-7),
                null, "eltesto");

        var deserializedToken = jwtBuilder.Decode<IDictionary<string, object>>(tokens[0]._Token);

        var sha256FromToken = deserializedToken["sha256"].ToString();
        var partnerFromToken = deserializedToken["partner"].ToString();

        var filesizeFromToken = long.Parse(deserializedToken["filesize"].ToString());

        using (var sha256 = SHA256.Create())
        {
            sha256String = HexStringFromBytes(sha256
                .ComputeHash(sampleGetter
                    .GetAsync(sha256FromToken, partnerFromToken).Result.FileStream));
        }

        Assert.Single(tokens);
        Assert.True(deserializedToken.ContainsKey("familyname"));
        Assert.Equal("131f95c51cc819465fa1797f6ccacf9d494aaaff46fa3eac73ae63ffbdfd8267", sha256String);
        Assert.Equal(69, filesizeFromToken);
    }
}
