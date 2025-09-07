# Services/StudentService/StudentService.API/Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy all project files first for better caching
COPY Services/StudentService/StudentService.API/*.csproj Services/StudentService/StudentService.API/
COPY Services/StudentService/StudentService.Infrastructure/*.csproj Services/StudentService/StudentService.Infrastructure/
COPY Services/StudentService/StudentService.Application/*.csproj Services/StudentService/StudentService.Application/
COPY Services/StudentService/StudentService.Domain/*.csproj Services/StudentService/StudentService.Domain/

# Copy BaseService dependencies
COPY Services/BaseService/BaseService.Infrastructure/*.csproj Services/BaseService/BaseService.Infrastructure/
COPY Services/BaseService/BaseService.Application/*.csproj Services/BaseService/BaseService.Application/
COPY Services/BaseService/BaseService.Domain/*.csproj Services/BaseService/BaseService.Domain/
COPY Services/BaseService/BaseService.Common/*.csproj Services/BaseService/BaseService.Common/

# Copy BuildingBlocks dependencies
COPY BuildingBlocks/BuildingBlocks/*.csproj BuildingBlocks/BuildingBlocks/
COPY BuildingBlocks/BuildingBlocks.Messaging/*.csproj BuildingBlocks/BuildingBlocks.Messaging/

RUN dotnet restore Services/StudentService/StudentService.API/StudentService.API.csproj

# Copy source files - ONLY copy what's needed for StudentService
COPY Services/StudentService/ Services/StudentService/
# Only copy BaseService source files (excluding BaseService.API to avoid conflicts)
COPY Services/BaseService/BaseService.Infrastructure/ Services/BaseService/BaseService.Infrastructure/
COPY Services/BaseService/BaseService.Application/ Services/BaseService/BaseService.Application/
COPY Services/BaseService/BaseService.Domain/ Services/BaseService/BaseService.Domain/
COPY Services/BaseService/BaseService.Common/ Services/BaseService/BaseService.Common/
COPY BuildingBlocks/ BuildingBlocks/

WORKDIR /src/Services/StudentService/StudentService.API
RUN dotnet build StudentService.API.csproj -c $BUILD_CONFIGURATION -o /app/build --no-warnings

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish StudentService.API.csproj -c $BUILD_CONFIGURATION -o /app/publish/studentservice /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish/studentservice .
ENTRYPOINT ["dotnet", "StudentService.API.dll"]