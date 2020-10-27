using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Services;
using Xunit;

namespace SampleExchangeApi.Console_Test
{
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
        private IContainerService _container;
        public readonly string IpAddress;

        public DockerFixture()
        {
            IpAddress = StartMongoDbContainer();
        }

        private string StartMongoDbContainer()
        {
            _container =
                new Builder().UseContainer()
                    .UseImage("mongo:xenial")
                    .ExposePort(27017, 27017)
                    .Build()
                    .Start();

            var containerIp = "127.0.0.1";

            Environment.SetEnvironmentVariable("MongoDb__ConnectionString", $"mongodb://{containerIp}:27017");

            Thread.Sleep(10000);
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
}