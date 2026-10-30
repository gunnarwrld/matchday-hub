# Multi-stage build for the Matchday Hub API.
# Stage 1 restores + publishes; stage 2 is a slim ASP.NET runtime image.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy only what the restore graph needs first, to maximise layer caching.
COPY global.json Directory.Build.props ./
COPY src/MatchdayHub.Domain/MatchdayHub.Domain.csproj src/MatchdayHub.Domain/
COPY src/MatchdayHub.Infrastructure/MatchdayHub.Infrastructure.csproj src/MatchdayHub.Infrastructure/
COPY src/MatchdayHub.Api/MatchdayHub.Api.csproj src/MatchdayHub.Api/
RUN dotnet restore src/MatchdayHub.Api/MatchdayHub.Api.csproj

COPY . .
RUN dotnet publish src/MatchdayHub.Api/MatchdayHub.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app ./
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "MatchdayHub.Api.dll"]
