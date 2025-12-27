FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["IstasyonDemo.Api.csproj", "."]
RUN dotnet restore "./IstasyonDemo.Api.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "IstasyonDemo.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "IstasyonDemo.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
# Curl kur, log klasörü oluştur ve yetkileri ayarla
RUN apt-get update && \
    apt-get install -y curl tzdata postgresql-client && \
    mkdir -p Logs Backups && \
    chown -R app:app /app

# Root olmayan kullanıcıya geç
USER app
ENTRYPOINT ["dotnet", "IstasyonDemo.Api.dll"]
