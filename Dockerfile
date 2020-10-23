FROM mcr.microsoft.com/dotnet/core/aspnet
COPY artifacts/SampleExchangeApi.Console/ /data
WORKDIR /data
ENTRYPOINT dotnet /data/SampleExchangeApi.Console.dll
