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

# Copy AiService projects
COPY Services/AiService/AiService.Domain/*.csproj Services/AiService/AiService.Domain/
COPY Services/AiService/AiService.Application/*.csproj Services/AiService/AiService.Application/
COPY Services/AiService/AiService.Infrastructure/*.csproj Services/AiService/AiService.Infrastructure/
COPY Services/AiService/AiService.API/*.csproj Services/AiService/AiService.API/

# Restore dependencies
RUN dotnet restore Services/AiService/AiService.API/AiService.API.csproj

# Copy source code
COPY BuildingBlocks/ BuildingBlocks/
COPY Services/BaseService/ Services/BaseService/
COPY Services/AiService/ Services/AiService/

# Build application
WORKDIR /src/Services/AiService/AiService.API
RUN dotnet build AiService.API.csproj -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish AiService.API.csproj -c $BUILD_CONFIGURATION -o /app/publish/AiService /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish/AiService .
ENTRYPOINT ["dotnet", "AiService.API.dll"]
