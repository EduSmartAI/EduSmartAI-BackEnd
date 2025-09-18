# Template Dockerfile tối ưu cho các .NET microservices
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

# SERVICE_SPECIFIC_PROJECTS_PLACEHOLDER

# Restore dependencies
RUN dotnet restore Services/{ServiceName}/{ServiceName}.API/{ServiceName}.API.csproj

# Copy source code
COPY BuildingBlocks/ BuildingBlocks/
COPY Services/BaseService/ Services/BaseService/
COPY Services/{ServiceName}/ Services/{ServiceName}/

# Build application
WORKDIR /src/Services/{ServiceName}/{ServiceName}.API
RUN dotnet build {ServiceName}.API.csproj -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish {ServiceName}.API.csproj -c $BUILD_CONFIGURATION -o /app/publish/{ServiceName} /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish/{ServiceName} .
ENTRYPOINT ["dotnet", "{ServiceName}.API.dll"]
