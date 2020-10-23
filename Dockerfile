FROM mcr.microsoft.com/dotnet/core/aspnet
COPY ./src/SampleExchangeApi.Console/bin/Release/netcoreapp3.1/ /data
WORKDIR /data
ENTRYPOINT dotnet /data/SampleExchangeApi.Console.dll
