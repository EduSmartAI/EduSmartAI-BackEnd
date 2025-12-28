#!/bin/bash

echo "🚀 EduSmart Deployment Script (Optimized)"

# Enable Docker BuildKit
export DOCKER_BUILDKIT=1
export COMPOSE_DOCKER_CLI_BUILD=1

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Check if we're doing a clean deploy or update
CLEAN_DEPLOY=${1:-false}

if [ "$CLEAN_DEPLOY" = "clean" ]; then
    print_warning "Performing CLEAN deployment - this will remove all containers and images"
    
    # Stop all containers
    docker compose down --volumes --remove-orphans
    
    # Clean up everything
    docker system prune -af
    docker builder prune -af
    
    print_status "Clean up completed"
else
    print_status "Performing SMART deployment - preserving cache when possible"
    
    # Stop containers gracefully
    docker compose down --remove-orphans
    
    # Only remove dangling images
    docker image prune -f --filter "dangling=true"
fi

# Build with optimal settings
print_status "Building services with parallel processing..."

# Build services in parallel with cache
docker compose build \
    --parallel \
    --progress=plain \
    --build-arg BUILDKIT_INLINE_CACHE=1

if [ $? -ne 0 ]; then
    print_error "Build failed!"
    exit 1
fi

print_status "Starting services..."

# Start services with health check timeout
docker compose up -d --wait --wait-timeout 180

if [ $? -ne 0 ]; then
    print_error "Failed to start services!"
    docker compose logs --tail=50
    exit 1
fi

# Wait a bit more for services to fully initialize
print_status "Waiting for services to initialize..."
sleep 30

# Health check
print_status "Performing health checks..."

services=("studentservice.api" "authservice.api" "quizservice.api" "teacherservice.api" "paymentservice.api" "notificationservice.api" "utilityservice.api" "courseservice.api" "aiservice.api" "reverseproxy")

failed_services=()

for service in "${services[@]}"; do
    if docker compose ps $service | grep -q -E "(healthy|Up.*healthy|Up \(healthy\))"; then
        print_status "✅ $service is healthy"
    elif docker compose ps $service | grep -q "Up"; then
        print_warning "⚠️  $service is running but health status unknown"
    else
        print_error "❌ $service failed to start"
        failed_services+=("$service")
    fi
done

# Show logs for failed services
if [ ${#failed_services[@]} -gt 0 ]; then
    print_error "The following services failed:"
    for service in "${failed_services[@]}"; do
        echo "--- Logs for $service ---"
        docker compose logs $service --tail=20
        echo ""
    done
fi

# Final status
print_status "📊 Final deployment status:"
docker compose ps

print_status "💾 Disk usage:"
df -h /var/lib/docker

print_status "🎉 Deployment script completed!"

if [ ${#failed_services[@]} -eq 0 ]; then
    print_status "All services are running successfully!"
    exit 0
else
    print_error "Some services failed to start properly. Check logs above."
    exit 1
fi
