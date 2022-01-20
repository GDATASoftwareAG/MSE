using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Net.Http;
using JWT.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using SampleExchangeApi.Console.ListRequester;
using SampleExchangeApi.Console.SampleDownload;
using Xunit;
using JWT.Algorithms;
using Microsoft.Extensions.Options;
using SampleExchangeApi.Console.Database;

namespace SampleExchangeApi.Console_Test;

[Collection("DockerContainerCollection")]
public class SampleExchangeTest : IClassFixture<DockerFixture>
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
                FilePath = Configuration["Config:FilePath"]
            }), new HttpClient());
    }
    private ISampleStorageHandler CreateSampleGetter()
    {
        var options = new StorageOptions();
        Configuration.GetSection("Storage").Bind(options);

        var sampleGetter = new SampleStorageHandler(Mock.Of<ILogger<SampleStorageHandler>>(), new OptionsWrapper<StorageOptions>(options));
        return sampleGetter;
    }

    private ListRequester CreateListRequester(ISampleMetadataHandler? sampleMetadataReader = null)
    {
        sampleMetadataReader ??= Mock.Of<ISampleMetadataHandler>();
        var options = new ListRequesterOptions();
        Configuration.GetSection("Token").Bind(options);
        return new ListRequester(Mock.Of<ILogger<ListRequester>>(), new OptionsWrapper<ListRequesterOptions>(options),
            sampleMetadataReader,
            CreatePartnerProvider(), CreateSampleGetter());
    }
    private MongoMetadataHandler CreateMongoMetadataReader()
    {
        var options = new MongoMetadataOptions();
        Configuration.GetSection("MongoDb").Bind(options);
        options.ConnectionString = $"mongodb://{_dockerFixture.IpAddress}:27017";
        return new MongoMetadataHandler(new OptionsWrapper<MongoMetadataOptions>(options));
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
        var partnerProvider = CreatePartnerProvider();

        Assert.False(partnerProvider
            .AreCredentialsOkay("ThisPartnerDoesNotExist", "FalschesPasswort"));
    }

    [Fact]
    public void AreCredentialsOkay_PartnerDoesExistButWrongPassword_ReturnsFalse()
    {
        var partnerProvider = CreatePartnerProvider();

        Assert.False(partnerProvider
            .AreCredentialsOkay("netisee", "FalschesPasswort"));
    }

    [Fact]
    public void AreCredentialsOkay_CredentialsAreOk_ReturnsTrue()
    {
        var partnerProvider = CreatePartnerProvider();

        Assert.True(partnerProvider
            .AreCredentialsOkay("partner2", "test123"));
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
                null);

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
            sha256String = HexStringFromBytes(await sha256
                .ComputeHashAsync((await sampleGetter.GetAsync(sha256FromToken, partnerFromToken)).FileStream));
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
                null);

        var deserializedToken = jwtBuilder.Decode<IDictionary<string, object>>(tokens[0]._Token);

        var sha256FromToken = deserializedToken["sha256"].ToString();
        var partnerFromToken = deserializedToken["partner"].ToString();

        var filesizeFromToken = long.Parse(deserializedToken["filesize"].ToString());

        using (var sha256 = SHA256.Create())
        {
            sha256String = HexStringFromBytes(await sha256
                .ComputeHashAsync((await sampleGetter
                    .GetAsync(sha256FromToken, partnerFromToken)).FileStream));
        }

        Assert.Single(tokens);
        Assert.True(deserializedToken.ContainsKey("familyname"));
        Assert.Equal("131f95c51cc819465fa1797f6ccacf9d494aaaff46fa3eac73ae63ffbdfd8267", sha256String);
        Assert.Equal(69, filesizeFromToken);
    }
}
