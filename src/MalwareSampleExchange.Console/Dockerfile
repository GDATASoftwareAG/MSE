FROM mcr.microsoft.com/dotnet/aspnet:6.0-alpine
COPY . /data
WORKDIR /data

ENTRYPOINT dotnet /data/MalwareSampleExchange.Console.dll

ENV ASPNETCORE_URLS="http://0.0.0.0:80"

LABEL org.opencontainers.image.title="Malware Sample Exchange (MSE) Image" \
      org.opencontainers.image.description="Malware Sample Exchange (MSE)" \
      org.opencontainers.image.url="https://github.com/GDATASoftwareAG/mse" \
      org.opencontainers.image.source="https://github.com/GDATASoftwareAG/mse/tree/master/src/MalwareSampleExchange.Console/" \
      org.opencontainers.image.license="MIT"
