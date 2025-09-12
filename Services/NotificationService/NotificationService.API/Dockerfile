FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files in dependency order for better layer caching
# Start with base dependencies (no internal project references)
COPY BuildingBlocks/BuildingBlocks/*.csproj BuildingBlocks/BuildingBlocks/
COPY BuildingBlocks/BuildingBlocks.Messaging/*.csproj BuildingBlocks/BuildingBlocks.Messaging/

# Copy BaseService projects (including API for shared AbstractControllers)
COPY Services/BaseService/BaseService.Domain/*.csproj Services/BaseService/BaseService.Domain/
COPY Services/BaseService/BaseService.Common/*.csproj Services/BaseService/BaseService.Common/
COPY Services/BaseService/BaseService.Application/*.csproj Services/BaseService/BaseService.Application/
COPY Services/BaseService/BaseService.Infrastructure/*.csproj Services/BaseService/BaseService.Infrastructure/
COPY Services/BaseService/BaseService.API/*.csproj Services/BaseService/BaseService.API/

# Copy QuizService projects
COPY Services/NotificationService/NotificationService.Domain/*.csproj Services/NotificationService/NotificationService.Domain/
COPY Services/NotificationService/NotificationService.Application/*.csproj Services/NotificationService/NotificationService.Application/
COPY Services/NotificationService/NotificationService.Infrastructure/*.csproj Services/NotificationService/NotificationService.Infrastructure/
COPY Services/NotificationService/NotificationService.API/*.csproj Services/NotificationService/NotificationService.API/

# Restore dependencies
RUN dotnet restore Services/NotificationService/NotificationService.API/NotificationService.API.csproj

# Copy source files in the same dependency order
COPY BuildingBlocks/ BuildingBlocks/

# Copy BaseService source (including API for shared AbstractControllers)
COPY Services/BaseService/BaseService.Domain/ Services/BaseService/BaseService.Domain/
COPY Services/BaseService/BaseService.Common/ Services/BaseService/BaseService.Common/
COPY Services/BaseService/BaseService.Application/ Services/BaseService/BaseService.Application/
COPY Services/BaseService/BaseService.Infrastructure/ Services/BaseService/BaseService.Infrastructure/
COPY Services/BaseService/BaseService.API/ Services/BaseService/BaseService.API/

# Copy NotificationService source
COPY Services/NotificationService/ Services/NotificationService/

WORKDIR /src/Services/NotificationService/NotificationService.API
RUN dotnet build NotificationService.API.csproj -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish NotificationService.API.csproj -c $BUILD_CONFIGURATION -o /app/publish/notificationservice /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish/notificationservice .
ENTRYPOINT ["dotnet", "NotificationService.API.dll"]
