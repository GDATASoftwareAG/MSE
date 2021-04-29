FROM mcr.microsoft.com/dotnet/aspnet:5.0
COPY ./src/SampleExchangeApi.Console/bin/Release/net5.0/ /data
WORKDIR /data
ENTRYPOINT dotnet /data/SampleExchangeApi.Console.dll
