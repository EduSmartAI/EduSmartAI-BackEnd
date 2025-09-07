FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy all project files first for better caching
COPY Services/BaseService/BaseService.API/*.csproj Services/BaseService/BaseService.API/
COPY Services/BaseService/BaseService.Infrastructure/*.csproj Services/BaseService/BaseService.Infrastructure/
COPY Services/BaseService/BaseService.Application/*.csproj Services/BaseService/BaseService.Application/
COPY Services/BaseService/BaseService.Domain/*.csproj Services/BaseService/BaseService.Domain/
COPY Services/BaseService/BaseService.Common/*.csproj Services/BaseService/BaseService.Common/

# Copy BuildingBlocks dependencies
COPY BuildingBlocks/BuildingBlocks/*.csproj BuildingBlocks/BuildingBlocks/
COPY BuildingBlocks/BuildingBlocks.Messaging/*.csproj BuildingBlocks/BuildingBlocks.Messaging/

RUN dotnet restore Services/BaseService/BaseService.API/BaseService.API.csproj

# Copy all source files
COPY Services/BaseService/ Services/BaseService/
COPY BuildingBlocks/ BuildingBlocks/

WORKDIR /src/Services/BaseService/BaseService.API
RUN dotnet build BaseService.API.csproj -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish BaseService.API.csproj -c $BUILD_CONFIGURATION -o /app/publish/baseservice /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish/baseservice .
ENTRYPOINT ["dotnet", "BaseService.API.dll"]
