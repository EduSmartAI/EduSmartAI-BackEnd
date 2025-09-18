FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy shared dependencies trước để tối ưu cache
COPY BuildingBlocks/BuildingBlocks/*.csproj BuildingBlocks/BuildingBlocks/
COPY BuildingBlocks/BuildingBlocks.Messaging/*.csproj BuildingBlocks/BuildingBlocks.Messaging/

# Copy BaseService projects
COPY Services/BaseService/BaseService.Domain/*.csproj Services/BaseService/BaseService.Domain/
COPY Services/BaseService/BaseService.Common/*.csproj Services/BaseService/BaseService.Common/
COPY Services/BaseService/BaseService.Application/*.csproj Services/BaseService/BaseService.Application/
COPY Services/BaseService/BaseService.Infrastructure/*.csproj Services/BaseService/BaseService.Infrastructure/
COPY Services/BaseService/BaseService.API/*.csproj Services/BaseService/BaseService.API/

# Copy UtilityService projects
COPY Services/UtilityService/UtilityService.Domain/*.csproj Services/UtilityService/UtilityService.Domain/
COPY Services/UtilityService/UtilityService.Application/*.csproj Services/UtilityService/UtilityService.Application/
COPY Services/UtilityService/UtilityService.Infrastructure/*.csproj Services/UtilityService/UtilityService.Infrastructure/
COPY Services/UtilityService/UtilityService.API/*.csproj Services/UtilityService/UtilityService.API/

# Restore dependencies
RUN dotnet restore Services/UtilityService/UtilityService.API/UtilityService.API.csproj

# Copy source code
COPY BuildingBlocks/ BuildingBlocks/
COPY Services/BaseService/ Services/BaseService/
COPY Services/UtilityService/ Services/UtilityService/

# Build application
WORKDIR /src/Services/UtilityService/UtilityService.API
RUN dotnet build UtilityService.API.csproj -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish UtilityService.API.csproj -c $BUILD_CONFIGURATION -o /app/publish/UtilityService /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish/UtilityService .
ENTRYPOINT ["dotnet", "UtilityService.API.dll"]
