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

# Copy PaymentService projects
COPY Services/PaymentService/PaymentService.Domain/*.csproj Services/PaymentService/PaymentService.Domain/
COPY Services/PaymentService/PaymentService.Application/*.csproj Services/PaymentService/PaymentService.Application/
COPY Services/PaymentService/PaymentService.Infrastructure/*.csproj Services/PaymentService/PaymentService.Infrastructure/
COPY Services/PaymentService/PaymentService.API/*.csproj Services/PaymentService/PaymentService.API/

# Restore dependencies
RUN dotnet restore Services/PaymentService/PaymentService.API/PaymentService.API.csproj

# Copy source code
COPY BuildingBlocks/ BuildingBlocks/
COPY Services/BaseService/ Services/BaseService/
COPY Services/PaymentService/ Services/PaymentService/

# Build application
WORKDIR /src/Services/PaymentService/PaymentService.API
RUN dotnet build PaymentService.API.csproj -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish PaymentService.API.csproj -c $BUILD_CONFIGURATION -o /app/publish/PaymentService /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish/PaymentService .
ENTRYPOINT ["dotnet", "PaymentService.API.dll"]
