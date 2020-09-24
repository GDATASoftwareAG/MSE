FROM artifactory.gdata.de:6555/dotnet/runtime:2.2
COPY artifacts/SampleExchangeApi.Console/ /data
WORKDIR /data
ENTRYPOINT dotnet /data/SampleExchangeApi.Console.dll
