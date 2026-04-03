# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY MovieTracker.API/MovieTracker.API.csproj MovieTracker.API/
RUN dotnet restore MovieTracker.API/MovieTracker.API.csproj

COPY MovieTracker.API/. MovieTracker.API/
WORKDIR /src/MovieTracker.API
RUN dotnet publish MovieTracker.API.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "MovieTracker.API.dll"]
