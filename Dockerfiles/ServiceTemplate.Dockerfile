# Template Dockerfile tối ưu cho các .NET microservices
# Thay thế {ServiceName} bằng tên service thực tế (VD: StudentService, AuthService, etc.)

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files theo thứ tự dependency để tối ưu cache
# 1. Copy shared dependencies trước (ít thay đổi)
COPY BuildingBlocks/BuildingBlocks/*.csproj BuildingBlocks/BuildingBlocks/
COPY BuildingBlocks/BuildingBlocks.Messaging/*.csproj BuildingBlocks/BuildingBlocks.Messaging/

# 2. Copy BaseService projects
COPY Services/BaseService/BaseService.Domain/*.csproj Services/BaseService/BaseService.Domain/
COPY Services/BaseService/BaseService.Common/*.csproj Services/BaseService/BaseService.Common/
COPY Services/BaseService/BaseService.Application/*.csproj Services/BaseService/BaseService.Application/
COPY Services/BaseService/BaseService.Infrastructure/*.csproj Services/BaseService/BaseService.Infrastructure/
COPY Services/BaseService/BaseService.API/*.csproj Services/BaseService/BaseService.API/

# 3. Copy service-specific projects (thay {ServiceName} bằng tên service thực tế)
COPY Services/{ServiceName}/{ServiceName}.Domain/*.csproj Services/{ServiceName}/{ServiceName}.Domain/
COPY Services/{ServiceName}/{ServiceName}.Application/*.csproj Services/{ServiceName}/{ServiceName}.Application/
COPY Services/{ServiceName}/{ServiceName}.Infrastructure/*.csproj Services/{ServiceName}/{ServiceName}.Infrastructure/
COPY Services/{ServiceName}/{ServiceName}.API/*.csproj Services/{ServiceName}/{ServiceName}.API/

# 4. Restore dependencies (layer này sẽ được cache nếu .csproj không thay đổi)
RUN dotnet restore Services/{ServiceName}/{ServiceName}.API/{ServiceName}.API.csproj

# 5. Copy source code (layer này thay đổi thường xuyên nhất)
COPY BuildingBlocks/ BuildingBlocks/
COPY Services/BaseService/ Services/BaseService/
COPY Services/{ServiceName}/ Services/{ServiceName}/

# 6. Build application
WORKDIR /src/Services/{ServiceName}/{ServiceName}.API
RUN dotnet build {ServiceName}.API.csproj -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish {ServiceName}.API.csproj -c $BUILD_CONFIGURATION -o /app/publish/{ServiceName} /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish/{ServiceName} .
ENTRYPOINT ["dotnet", "{ServiceName}.API.dll"]
