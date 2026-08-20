# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS restore
WORKDIR /src

COPY ["Common/Auth/Auth.csproj", "Common/Auth/"]
COPY ["Common/Shared/Shared.csproj", "Common/Shared/"]
COPY ["Modules/WBand.Modules.BandModule/WBand.Modules.BandModule.csproj", "Modules/WBand.Modules.BandModule/"]
COPY ["Modules/WBand.Modules.FileModule/WBand.Modules.FileModule.csproj", "Modules/WBand.Modules.FileModule/"]
COPY ["Modules/WBand.Modules.MixerModule/WBand.Modules.MixerModule.csproj", "Modules/WBand.Modules.MixerModule/"]
COPY ["Modules/WBand.Modules.PlaylistModule/WBand.Modules.PlaylistModule.csproj", "Modules/WBand.Modules.PlaylistModule/"]
COPY ["Modules/WBand.Modules.SongModule/WBand.Modules.SongModule.csproj", "Modules/WBand.Modules.SongModule/"]
COPY ["Modules/WBand.Modules.StemModule/WBand.Modules.StemModule.csproj", "Modules/WBand.Modules.StemModule/"]
COPY ["Modules/WBand.Modules.UserModule/WBand.Modules.UserModule.csproj", "Modules/WBand.Modules.UserModule/"]
COPY ["App/WBand.WebAPI/WBand.WebAPI.csproj", "App/WBand.WebAPI/"]
RUN dotnet restore "App/WBand.WebAPI/WBand.WebAPI.csproj"

FROM restore AS publish
COPY . .
RUN dotnet publish "App/WBand.WebAPI/WBand.WebAPI.csproj" \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_ENVIRONMENT=Production
EXPOSE 8080
COPY --from=publish --chown=$APP_UID:$APP_UID /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "WBand.WebAPI.dll"]
