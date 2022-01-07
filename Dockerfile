FROM mcr.microsoft.com/dotnet/aspnet:6.0-alpine
COPY ./src/SampleExchangeApi.Console/bin/Release/net6.0/ /data
WORKDIR /data
ENTRYPOINT dotnet /data/SampleExchangeApi.Console.dll
