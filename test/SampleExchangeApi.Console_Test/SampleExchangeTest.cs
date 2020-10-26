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

namespace SampleExchangeApi.Console_Test
{
    [Collection("DockerContainerCollection")]
    public class SampleExchangeTest
    {
        private readonly DockerFixture _dockerFixture;
        private static readonly IConfiguration Configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();

        private static readonly ILogger Logger = LoggerFactory.Create(builder =>
        {
            builder.AddFilter("Logger", LogLevel.Information);
            builder.AddConsole();
        }).CreateLogger("Logger");

        public SampleExchangeTest(DockerFixture dockerFixture)
        {
            _dockerFixture = dockerFixture;
        }

        private static Settings GetShareConfig()
        {
            var document = File.ReadAllText(Configuration["Config:YAML"]);
            var input = new StringReader(document);

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(PascalCaseNamingConvention.Instance)
                .Build();

            return deserializer.Deserialize<Settings>(input);
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
                    SampleSet = "Classic"
                });
            mongoCollection.InsertOne(
                new ExportSample
                {
                    Sha256SampleSet = "cda0a81901ced9306d023500ff1c383d6b4bd8cebefa886faa2a627a796e87f:Classic",
                    Sha256 = "cda0a81901ced9306d023500ff1c383d6b4bd8cebefa886faa2a627a796e87f",
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

        [Fact]
        public void AreCredentialsOkay_PartnerDoesNotExist_ReturnsFalse()
        {
            var fakeMetaDataReader = new Mock<ISampleMetadataReader>();
            var listRequester =
                new ListRequester(Configuration, Logger, fakeMetaDataReader.Object, GetShareConfig());

            Assert.False(listRequester
                .AreCredentialsOkay("ThisPartnerDoesNotExist", "FalschesPasswort", "eltesto"));
        }

        [Fact]
        public void AreCredentialsOkay_PartnerDoesExistButWrongPassword_ReturnsFalse()
        {
            var fakeMetaDataReader = new Mock<ISampleMetadataReader>();
            var listRequester = new ListRequester(Configuration, Logger, fakeMetaDataReader.Object,
                GetShareConfig());

            Assert.False(listRequester
                .AreCredentialsOkay("netisee", "FalschesPasswort", "eltesto"));
        }

        [Fact]
        public void AreCredentialsOkay_CredentialsAreOk_ReturnsTrue()
        {
            var fakeMetaDataReader = new Mock<ISampleMetadataReader>();
            var listRequester = new ListRequester(Configuration, Logger, fakeMetaDataReader.Object,
                GetShareConfig());

            Assert.True(listRequester
                .AreCredentialsOkay("GanzTollerTauschPartner", "test123", "eltesto"));
        }

        [Fact]
        public async void BusinessLogicCallback_GetSampleToken()
        {
            string sha256String;
            var mongoClient = new MongoClient($"mongodb://{_dockerFixture.IpAddress}:27017");
            var listRequester = new ListRequester(Configuration, Logger,
                new MongoMetadataReader(Configuration, mongoClient, Logger), GetShareConfig());

            var sampleGetter = new SampleGetter(Configuration, Logger);

            WriteFakeDataIntoTestMongo(mongoClient);
            CreateTestFile();

            var tokens = await listRequester
                .RequestListAsync("GanzTollerTauschPartner", DateTime.Now.AddDays(-7),
                    null, "eltesto");

            var deserializedToken = new JwtBuilder()
                .WithAlgorithm(new HMACSHA512Algorithm()) // symmetric
                .WithSecret(Configuration["Token:Secret"])
                .MustVerifySignature()
                .Decode<IDictionary<string, object> > (tokens[0]._Token);


            //var deserializedToken = jwtBuilder.Decode<IDictionary<string, object>>(tokens[0]._Token, Configuration["Token:Secret"], verify: true);
            
            var sha256FromToken = deserializedToken["sha256"].ToString();
            var partnerFromToken = deserializedToken["partner"].ToString();
            var filesizeFromToken = long.Parse(deserializedToken["filesize"].ToString());

            using (var sha256 = SHA256.Create())
            {
                sha256String = HexStringFromBytes(sha256
                    .ComputeHash(sampleGetter
                        .Get(sha256FromToken, partnerFromToken, "eltesto").FileStream));
            }

           // Assert.Single(tokens.Result);
            Assert.Equal("131f95c51cc819465fa1797f6ccacf9d494aaaff46fa3eac73ae63ffbdfd8267", sha256String);
            Assert.Equal(69,filesizeFromToken);
        }
    }
}