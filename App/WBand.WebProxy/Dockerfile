# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS restore
WORKDIR /src
COPY ["App/WBand.WebProxy/WBand.WebProxy.csproj", "App/WBand.WebProxy/"]
RUN --mount=type=secret,id=nuget_config,target=/root/.nuget/NuGet/NuGet.Config,required=true \
    dotnet restore "App/WBand.WebProxy/WBand.WebProxy.csproj"

FROM restore AS publish
COPY App/WBand.WebProxy/ App/WBand.WebProxy/
RUN dotnet publish "App/WBand.WebProxy/WBand.WebProxy.csproj" \
    --configuration Release --no-restore --output /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080 DOTNET_ENVIRONMENT=Production
EXPOSE 8080
COPY --from=publish --chown=$APP_UID:$APP_UID /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "WBand.WebProxy.dll"]
