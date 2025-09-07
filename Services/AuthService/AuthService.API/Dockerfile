FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy all project files first for better caching
COPY Services/AuthService/AuthService.API/*.csproj Services/AuthService/AuthService.API/
COPY Services/AuthService/AuthService.Infrastructure/*.csproj Services/AuthService/AuthService.Infrastructure/
COPY Services/AuthService/AuthService.Application/*.csproj Services/AuthService/AuthService.Application/
COPY Services/AuthService/AuthService.Domain/*.csproj Services/AuthService/AuthService.Domain/

# Copy BaseService dependencies
COPY Services/BaseService/BaseService.API/*.csproj Services/BaseService/BaseService.API/
COPY Services/BaseService/BaseService.Infrastructure/*.csproj Services/BaseService/BaseService.Infrastructure/
COPY Services/BaseService/BaseService.Application/*.csproj Services/BaseService/BaseService.Application/
COPY Services/BaseService/BaseService.Domain/*.csproj Services/BaseService/BaseService.Domain/
COPY Services/BaseService/BaseService.Common/*.csproj Services/BaseService/BaseService.Common/

# Copy BuildingBlocks dependencies
COPY BuildingBlocks/BuildingBlocks/*.csproj BuildingBlocks/BuildingBlocks/
COPY BuildingBlocks/BuildingBlocks.Messaging/*.csproj BuildingBlocks/BuildingBlocks.Messaging/

RUN dotnet restore Services/AuthService/AuthService.API/AuthService.API.csproj

# Copy all source files
COPY Services/AuthService/ Services/AuthService/
COPY Services/BaseService/ Services/BaseService/
COPY BuildingBlocks/ BuildingBlocks/

WORKDIR /src/Services/AuthService/AuthService.API
RUN dotnet build AuthService.API.csproj -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish AuthService.API.csproj -c $BUILD_CONFIGURATION -o /app/publish/authservice /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish/authservice .
ENTRYPOINT ["dotnet", "AuthService.API.dll"]