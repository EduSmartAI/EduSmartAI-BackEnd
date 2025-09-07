# Services/StudentService/StudentService.API/Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy entire source first
COPY . .

# Restore packages for StudentService
RUN dotnet restore Services/StudentService/StudentService.API/StudentService.API.csproj

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Publish only StudentService with specific output directory to avoid conflicts
RUN dotnet publish Services/StudentService/StudentService.API/StudentService.API.csproj \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    /p:UseAppHost=false \
    /p:TreatWarningsAsErrors=false \
    /p:PublishSingleFile=false \
    --no-restore

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "StudentService.API.dll"]