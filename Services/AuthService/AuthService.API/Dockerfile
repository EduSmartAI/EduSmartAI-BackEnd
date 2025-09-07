FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY Services/AuthService/AuthService.API/*.csproj Services/AuthService/AuthService.API/
COPY Services/AuthService/AuthService.Infrastructure/*.csproj Services/AuthService/AuthService.Infrastructure/
COPY Services/AuthService/AuthService.Application/*.csproj Services/AuthService/AuthService.Application/

RUN dotnet restore Services/AuthService/AuthService.API/AuthService.API.csproj

COPY Services/AuthService/AuthService.API/ Services/AuthService/AuthService.API/
COPY Services/AuthService/AuthService.Infrastructure/ Services/AuthService/AuthService.Infrastructure/
COPY Services/AuthService/AuthService.Application/*.csproj Services/AuthService/AuthService.Application/

WORKDIR /src/Services/AuthService/AuthService.API
RUN dotnet build AuthService.API.csproj -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish AuthService.API.csproj -c $BUILD_CONFIGURATION -o /app/publish/authservice /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish/authservice .
ENTRYPOINT ["dotnet", "AuthService.API.dll"]