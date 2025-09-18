#!/bin/bash

# Script tự động tạo/cập nhật Dockerfile cho tất cả services
echo "🔧 Updating Dockerfiles for all services..."

# Danh sách các services cần cập nhật
services=("StudentService" "AuthService" "TeacherService" "PaymentService" "NotificationService" "UtilityService" "AiService")

# Function để tạo Dockerfile cho service thông thường
create_dockerfile() {
    local service_name=$1
    local dockerfile_path="Services/${service_name}/${service_name}.API/Dockerfile"
    
    echo "📝 Creating Dockerfile for $service_name..."
    
    # Tạo thư mục nếu chưa có
    mkdir -p "Services/${service_name}/${service_name}.API"
    
    # Tạo Dockerfile
    cat > "$dockerfile_path" << 'DOCKERFILE_END'
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

# Copy SERVICE_NAME projects
COPY Services/SERVICE_NAME/SERVICE_NAME.Domain/*.csproj Services/SERVICE_NAME/SERVICE_NAME.Domain/
COPY Services/SERVICE_NAME/SERVICE_NAME.Application/*.csproj Services/SERVICE_NAME/SERVICE_NAME.Application/
COPY Services/SERVICE_NAME/SERVICE_NAME.Infrastructure/*.csproj Services/SERVICE_NAME/SERVICE_NAME.Infrastructure/
COPY Services/SERVICE_NAME/SERVICE_NAME.API/*.csproj Services/SERVICE_NAME/SERVICE_NAME.API/

# Restore dependencies
RUN dotnet restore Services/SERVICE_NAME/SERVICE_NAME.API/SERVICE_NAME.API.csproj

# Copy source code
COPY BuildingBlocks/ BuildingBlocks/
COPY Services/BaseService/ Services/BaseService/
COPY Services/SERVICE_NAME/ Services/SERVICE_NAME/

# Build application
WORKDIR /src/Services/SERVICE_NAME/SERVICE_NAME.API
RUN dotnet build SERVICE_NAME.API.csproj -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish SERVICE_NAME.API.csproj -c $BUILD_CONFIGURATION -o /app/publish/SERVICE_NAME /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish/SERVICE_NAME .
ENTRYPOINT ["dotnet", "SERVICE_NAME.API.dll"]
DOCKERFILE_END

    # Thay thế SERVICE_NAME bằng tên service thực tế
    sed -i.bak "s/SERVICE_NAME/${service_name}/g" "$dockerfile_path"
    rm "${dockerfile_path}.bak"
    
    echo "✅ Created: $dockerfile_path"
}

# Function để tạo Dockerfile cho CourseService
create_course_dockerfile() {
    local dockerfile_path="Services/CourseService/Course.API/Dockerfile"
    
    echo "📝 Creating Dockerfile for CourseService (special case)..."
    
    # Tạo thư mục nếu chưa có
    mkdir -p "Services/CourseService/Course.API"
    
    # Tạo Dockerfile cho CourseService
    cat > "$dockerfile_path" << 'DOCKERFILE_END'
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
DOCKERFILE_END

    echo "✅ Created: $dockerfile_path"
}

# Tạo Dockerfile cho tất cả services
for service in "${services[@]}"; do
    create_dockerfile "$service"
done

# Tạo Dockerfile cho CourseService
create_course_dockerfile

echo ""
echo "🎉 All Dockerfiles have been created successfully!"
echo ""
echo "📋 Created files:"
for service in "${services[@]}"; do
    echo "   - Services/${service}/${service}.API/Dockerfile"
done
echo "   - Services/CourseService/Course.API/Dockerfile"

echo ""
echo "💡 Next steps:"
echo "   1. Test build one service: docker build -f Services/StudentService/StudentService.API/Dockerfile . -t studentservice"
echo "   2. Test build all: docker compose build --parallel"
echo "   3. Deploy: ./deploy.sh"
