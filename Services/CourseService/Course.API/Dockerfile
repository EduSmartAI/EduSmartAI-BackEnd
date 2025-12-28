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

# Copy CourseService projects
COPY Services/CourseService/Course.Domain/*.csproj Services/CourseService/Course.Domain/
COPY Services/CourseService/Course.Application/*.csproj Services/CourseService/Course.Application/
COPY Services/CourseService/Course.Infrastructure/*.csproj Services/CourseService/Course.Infrastructure/
COPY Services/CourseService/Course.API/*.csproj Services/CourseService/Course.API/

# Restore dependencies
RUN dotnet restore Services/CourseService/Course.API/Course.API.csproj

# Copy source code
COPY BuildingBlocks/ BuildingBlocks/
COPY Services/BaseService/ Services/BaseService/
COPY Services/CourseService/ Services/CourseService/

# Build application
WORKDIR /src/Services/CourseService/Course.API
RUN dotnet build Course.API.csproj -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish Course.API.csproj -c $BUILD_CONFIGURATION -o /app/publish/CourseService /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish/CourseService .
ENTRYPOINT ["dotnet", "Course.API.dll"]
