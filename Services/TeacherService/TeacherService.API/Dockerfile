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

# Copy TeacherService projects
COPY Services/TeacherService/TeacherService.Domain/*.csproj Services/TeacherService/TeacherService.Domain/
COPY Services/TeacherService/TeacherService.Application/*.csproj Services/TeacherService/TeacherService.Application/
COPY Services/TeacherService/TeacherService.Infrastructure/*.csproj Services/TeacherService/TeacherService.Infrastructure/
COPY Services/TeacherService/TeacherService.API/*.csproj Services/TeacherService/TeacherService.API/

# Restore dependencies
RUN dotnet restore Services/TeacherService/TeacherService.API/TeacherService.API.csproj

# Copy source files in the same dependency order
COPY BuildingBlocks/ BuildingBlocks/

# Copy BaseService source (including API for shared AbstractControllers)
COPY Services/BaseService/BaseService.Domain/ Services/BaseService/BaseService.Domain/
COPY Services/BaseService/BaseService.Common/ Services/BaseService/BaseService.Common/
COPY Services/BaseService/BaseService.Application/ Services/BaseService/BaseService.Application/
COPY Services/BaseService/BaseService.Infrastructure/ Services/BaseService/BaseService.Infrastructure/
COPY Services/BaseService/BaseService.API/ Services/BaseService/BaseService.API/

# Copy TeacherService source
COPY Services/TeacherService/ Services/TeacherService/

WORKDIR /src/Services/TeacherService/TeacherService.API
RUN dotnet build TeacherService.API.csproj -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish TeacherService.API.csproj -c $BUILD_CONFIGURATION -o /app/publish/teacherservice /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish/teacherservice .
ENTRYPOINT ["dotnet", "TeacherService.API.dll"]
