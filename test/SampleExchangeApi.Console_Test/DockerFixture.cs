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
                    .Build()
                    .Start();

            var containerIp = _container.GetConfiguration().NetworkSettings.IPAddress;

            Environment.SetEnvironmentVariable("MongoDb__ConnectionString", $"mongodb://{containerIp}:27017");

            Thread.Sleep(10000);
            return _container.GetConfiguration().NetworkSettings.IPAddress;
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