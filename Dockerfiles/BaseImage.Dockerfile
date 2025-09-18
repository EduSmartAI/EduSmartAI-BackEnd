# Base image chứa các shared components
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS base-builder
WORKDIR /src

# Copy và restore shared dependencies trước
COPY BuildingBlocks/BuildingBlocks/*.csproj BuildingBlocks/BuildingBlocks/
COPY BuildingBlocks/BuildingBlocks.Messaging/*.csproj BuildingBlocks/BuildingBlocks.Messaging/
COPY Services/BaseService/BaseService.Domain/*.csproj Services/BaseService/BaseService.Domain/
COPY Services/BaseService/BaseService.Common/*.csproj Services/BaseService/BaseService.Common/
COPY Services/BaseService/BaseService.Application/*.csproj Services/BaseService/BaseService.Application/
COPY Services/BaseService/BaseService.Infrastructure/*.csproj Services/BaseService/BaseService.Infrastructure/
COPY Services/BaseService/BaseService.API/*.csproj Services/BaseService/BaseService.API/

# Restore shared dependencies
RUN dotnet restore BuildingBlocks/BuildingBlocks/BuildingBlocks.csproj && \
    dotnet restore BuildingBlocks/BuildingBlocks.Messaging/BuildingBlocks.Messaging.csproj && \
    dotnet restore Services/BaseService/BaseService.Domain/BaseService.Domain.csproj && \
    dotnet restore Services/BaseService/BaseService.Common/BaseService.Common.csproj && \
    dotnet restore Services/BaseService/BaseService.Application/BaseService.Application.csproj && \
    dotnet restore Services/BaseService/BaseService.Infrastructure/BaseService.Infrastructure.csproj && \
    dotnet restore Services/BaseService/BaseService.API/BaseService.API.csproj

# Copy source code của shared components
COPY BuildingBlocks/ BuildingBlocks/
COPY Services/BaseService/ Services/BaseService/

# Build shared components
RUN dotnet build BuildingBlocks/BuildingBlocks/BuildingBlocks.csproj -c Release && \
    dotnet build BuildingBlocks/BuildingBlocks.Messaging/BuildingBlocks.Messaging.csproj -c Release && \
    dotnet build Services/BaseService/BaseService.Domain/BaseService.Domain.csproj -c Release && \
    dotnet build Services/BaseService/BaseService.Common/BaseService.Common.csproj -c Release && \
    dotnet build Services/BaseService/BaseService.Application/BaseService.Application.csproj -c Release && \
    dotnet build Services/BaseService/BaseService.Infrastructure/BaseService.Infrastructure.csproj -c Release && \
    dotnet build Services/BaseService/BaseService.API/BaseService.API.csproj -c Release
