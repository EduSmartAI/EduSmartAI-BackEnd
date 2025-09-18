#!/bin/bash

# Script tự động tạo/cập nhật Dockerfile cho tất cả services
# Sử dụng template đã tối ưu

echo "🔧 Updating Dockerfiles for all services..."

# Danh sách các services cần cập nhật
services=("StudentService" "AuthService" "TeacherService" "PaymentService" "NotificationService" "UtilityService" "AiService" "GatewayService" "QuizService")

# CourseService có cấu trúc hơi khác (Course.API thay vì CourseService.API)
special_services=("CourseService:Course")

# Template path
template_file="Dockerfiles/ServiceTemplate.Dockerfile"

if [ ! -f "$template_file" ]; then
    echo "❌ Template file not found: $template_file"
    exit 1
fi

# Function để tạo Dockerfile từ template
create_dockerfile() {
    local service_name=$1
    local api_folder_name=${2:-$service_name}
    local dockerfile_path="Services/${service_name}/${api_folder_name}.API/Dockerfile"
    
    echo "📝 Creating/updating Dockerfile for $service_name..."
    
    # Tạo thư mục nếu chưa có
    mkdir -p "Services/${service_name}/${api_folder_name}.API"
    
    # Thay thế {ServiceName} trong template và tạo Dockerfile
    sed "s/{ServiceName}/${service_name}/g" "$template_file" > "$dockerfile_path"
    
    # Nếu là CourseService, cần điều chỉnh đường dẫn
    if [ "$service_name" = "CourseService" ]; then
        sed -i '' "s/Course\.API/Course.API/g" "$dockerfile_path"
        sed -i '' "s/CourseService\.API/Course.API/g" "$dockerfile_path"
    fi
    
    echo "✅ Updated: $dockerfile_path"
}

# Cập nhật các services thông thường
for service in "${services[@]}"; do
    create_dockerfile "$service"
done

# Cập nhật CourseService (trường hợp đặc biệt)
create_dockerfile "CourseService" "Course"

echo ""
echo "🎉 All Dockerfiles have been updated!"
echo ""
echo "📋 Summary of updated files:"
for service in "${services[@]}"; do
    echo "   - Services/${service}/${service}.API/Dockerfile"
done
echo "   - Services/CourseService/Course.API/Dockerfile"

echo ""
echo "💡 Next steps:"
echo "   1. Review the generated Dockerfiles"
echo "   2. Test build: docker compose build --parallel"
echo "   3. Deploy: ./deploy.sh"
