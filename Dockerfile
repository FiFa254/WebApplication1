FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore first so this layer is cached until a project file changes.
COPY DevFolio.csproj ./
COPY DevFolio.PostgresMigrations/DevFolio.PostgresMigrations.csproj ./DevFolio.PostgresMigrations/
RUN dotnet restore DevFolio.PostgresMigrations/DevFolio.PostgresMigrations.csproj

COPY . ./
RUN dotnet publish DevFolio.csproj -c Release -o /app/publish --no-restore \
 && dotnet build DevFolio.PostgresMigrations/DevFolio.PostgresMigrations.csproj -c Release --no-restore \
 && cp DevFolio.PostgresMigrations/bin/Release/net8.0/DevFolio.PostgresMigrations.dll /app/publish/

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    Storage__UploadsPath=/data/uploads \
    DataProtection__KeysPath=/data/keys

# /data holds uploaded images and data protection keys; mount a volume there.
RUN mkdir -p /data/uploads /data/keys && chown -R app:app /data
VOLUME ["/data"]

COPY --from=build /app/publish .

USER app
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=10s --start-period=30s --retries=3 \
    CMD ["dotnet", "DevFolio.dll", "healthcheck"]

ENTRYPOINT ["dotnet", "DevFolio.dll"]
