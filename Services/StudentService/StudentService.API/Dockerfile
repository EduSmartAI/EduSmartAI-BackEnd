FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY Services/StudentService/StudentService.API/*.csproj Services/StudentService/StudentService.API/
COPY Services/StudentService/StudentService.Infrastructure/*.csproj Services/StudentService/StudentService.Infrastructure/
COPY Services/StudentService/StudentService.Application/*.csproj Services/StudentService/StudentService.Application/

RUN dotnet restore Services/StudentService/StudentService.API/StudentService.API.csproj

COPY Services/StudentService/StudentService.API/ Services/StudentService/StudentService.API/
COPY Services/StudentService/StudentService.Infrastructure/ Services/StudentService/StudentService.Infrastructure/
COPY Services/StudentService/StudentService.Application/*.csproj Services/StudentService/StudentService.Application/

WORKDIR /src/Services/StudentService/StudentService.API
RUN dotnet build StudentService.API.csproj -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish StudentService.API.csproj -c $BUILD_CONFIGURATION -o /app/publish/studentservice /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish/studentservice .
ENTRYPOINT ["dotnet", "StudentService.API.dll"]