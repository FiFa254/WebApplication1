FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY WebApplication1.csproj ./
COPY WebApplication1.PostgresMigrations/WebApplication1.PostgresMigrations.csproj ./WebApplication1.PostgresMigrations/
RUN dotnet restore WebApplication1.csproj

COPY . ./
RUN dotnet publish WebApplication1.csproj -c Release -o /app/publish
RUN dotnet build WebApplication1.PostgresMigrations/WebApplication1.PostgresMigrations.csproj -c Release --no-restore
RUN cp WebApplication1.PostgresMigrations/bin/Release/net8.0/WebApplication1.PostgresMigrations.dll /app/publish/

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "WebApplication1.dll"]
