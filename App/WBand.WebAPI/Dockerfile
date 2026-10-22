# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS restore
WORKDIR /src
COPY ["Modules/WBand.Modules.EventModule/WBand.Modules.EventModule.csproj", "Modules/WBand.Modules.EventModule/"]
COPY ["Modules/WBand.Modules.BandModule/Contracts/WBand.Modules.BandModule.Contracts.csproj", "Modules/WBand.Modules.BandModule/Contracts/"]
COPY ["Modules/WBand.Modules.BandModule/WBand.Modules.BandModule.csproj", "Modules/WBand.Modules.BandModule/"]
COPY ["App/WBand.WebAPI/WBand.WebAPI.csproj", "App/WBand.WebAPI/"]
RUN dotnet restore "App/WBand.WebAPI/WBand.WebAPI.csproj"

FROM restore AS publish
COPY Modules/WBand.Modules.EventModule/ Modules/WBand.Modules.EventModule/
COPY Modules/WBand.Modules.BandModule/ Modules/WBand.Modules.BandModule/
COPY App/WBand.WebAPI/ App/WBand.WebAPI/
RUN dotnet publish "App/WBand.WebAPI/WBand.WebAPI.csproj" \
    --configuration Release --no-restore --output /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080 DOTNET_ENVIRONMENT=Production
EXPOSE 8080
COPY --from=publish --chown=$APP_UID:$APP_UID /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "WBand.WebAPI.dll"]
