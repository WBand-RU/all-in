# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS restore
WORKDIR /src

COPY ["Common/Shared/Shared.csproj", "Common/Shared/"]
COPY ["Modules/WBand.Modules.BandModule/WBand.Modules.BandModule.csproj", "Modules/WBand.Modules.BandModule/"]
COPY ["Modules/WBand.Modules.FileModule/WBand.Modules.FileModule.csproj", "Modules/WBand.Modules.FileModule/"]
COPY ["Modules/WBand.Modules.MixerModule/WBand.Modules.MixerModule.csproj", "Modules/WBand.Modules.MixerModule/"]
COPY ["Modules/WBand.Modules.PlaylistModule/WBand.Modules.PlaylistModule.csproj", "Modules/WBand.Modules.PlaylistModule/"]
COPY ["Modules/WBand.Modules.SongModule/WBand.Modules.SongModule.csproj", "Modules/WBand.Modules.SongModule/"]
COPY ["Modules/WBand.Modules.StemModule/WBand.Modules.StemModule.csproj", "Modules/WBand.Modules.StemModule/"]
COPY ["App/WBand.MixerWorker/WBand.MixerWorker.csproj", "App/WBand.MixerWorker/"]
RUN dotnet restore "App/WBand.MixerWorker/WBand.MixerWorker.csproj"

FROM restore AS publish
COPY . .
RUN dotnet publish "App/WBand.MixerWorker/WBand.MixerWorker.csproj" \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
RUN apt-get update \
    && apt-get install --yes --no-install-recommends ffmpeg \
    && rm -rf /var/lib/apt/lists/*
ENV DOTNET_ENVIRONMENT=Production
COPY --from=publish --chown=$APP_UID:$APP_UID /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "WBand.MixerWorker.dll"]
