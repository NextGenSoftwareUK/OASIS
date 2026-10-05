#!/bin/bash

# Lumina J5 Bootstrap Deployment Script
# This script handles deployment of the Lumina J5 Bootstrap system

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
PROJECT_NAME="lumina-j5-bootstrap"
NAMESPACE="lumina"
ENVIRONMENT="${ENVIRONMENT:-development}"
REGISTRY="${REGISTRY:-localhost:5000}"
VERSION="${VERSION:-latest}"

# Functions
log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

check_prerequisites() {
    log_info "Checking prerequisites..."
    
    # Check if Docker is installed and running
    if ! command -v docker &> /dev/null; then
        log_error "Docker is not installed"
        exit 1
    fi
    
    if ! docker info &> /dev/null; then
        log_error "Docker is not running"
        exit 1
    fi
    
    # Check if Docker Compose is installed
    if ! command -v docker-compose &> /dev/null; then
        log_error "Docker Compose is not installed"
        exit 1
    fi
    
    log_success "Prerequisites check completed"
}

build_image() {
    log_info "Building Docker image..."
    
    cd bootcode
    
    # Build the image
    docker build -t ${REGISTRY}/${PROJECT_NAME}:${VERSION} .
    
    # Tag as latest if not already
    if [ "$VERSION" != "latest" ]; then
        docker tag ${REGISTRY}/${PROJECT_NAME}:${VERSION} ${REGISTRY}/${PROJECT_NAME}:latest
    fi
    
    cd ..
    
    log_success "Docker image built successfully"
}

deploy_docker_compose() {
    log_info "Deploying with Docker Compose..."
    
    # Create environment file
    cat > .env << EOF
ENVIRONMENT=${ENVIRONMENT}
VERSION=${VERSION}
REGISTRY=${REGISTRY}
PROJECT_NAME=${PROJECT_NAME}
EOF
    
    # Start services
    docker-compose up -d
    
    # Wait for services to be ready
    log_info "Waiting for services to be ready..."
    sleep 30
    
    # Check service health
    check_service_health
    
    log_success "Docker Compose deployment completed"
}

check_service_health() {
    log_info "Checking service health..."
    
    # Check main service
    local max_attempts=30
    local attempt=1
    
    while [ $attempt -le $max_attempts ]; do
        if curl -f http://localhost:8080/healthz &> /dev/null; then
            log_success "Main service is healthy"
            break
        fi
        
        if [ $attempt -eq $max_attempts ]; then
            log_error "Main service health check failed after $max_attempts attempts"
            return 1
        fi
        
        log_info "Health check attempt $attempt/$max_attempts - waiting..."
        sleep 10
        ((attempt++))
    done
    
    log_success "All services are healthy"
}

show_status() {
    log_info "Deployment Status:"
    echo ""
    
    # Docker Compose status
    echo "Docker Compose Services:"
    docker-compose ps
    
    echo ""
    
    # Service URLs
    echo "Service URLs:"
    echo "  - Lumina J5 API: http://localhost:8080"
    echo "  - Grafana: http://localhost:3000 (admin/admin123)"
    echo "  - Prometheus: http://localhost:9090"
    echo "  - MinIO: http://localhost:9001 (minioadmin/minioadmin123)"
    echo "  - Vault: http://localhost:8200 (root token: root)"
    
    echo ""
    
    # Health check
    if curl -f http://localhost:8080/healthz &> /dev/null; then
        echo "✅ Main service is healthy"
    else
        echo "❌ Main service is not responding"
    fi
}

show_help() {
    echo "Lumina J5 Bootstrap Deployment Script"
    echo ""
    echo "Usage: $0 [COMMAND] [OPTIONS]"
    echo ""
    echo "Commands:"
    echo "  build       Build Docker image"
    echo "  deploy      Deploy the application"
    echo "  status      Show deployment status"
    echo "  help        Show this help message"
    echo ""
    echo "Options:"
    echo "  -e, --environment ENV    Set environment (development|staging|production)"
    echo "  -v, --version VERSION    Set version tag"
    echo "  -r, --registry REGISTRY  Set Docker registry"
    echo ""
    echo "Examples:"
    echo "  $0 build"
    echo "  $0 deploy -e production -v 1.0.0"
    echo "  $0 status"
}

# Parse command line arguments
COMMAND=""
while [[ $# -gt 0 ]]; do
    case $1 in
        build|deploy|status|help)
            COMMAND="$1"
            shift
            ;;
        -e|--environment)
            ENVIRONMENT="$2"
            shift 2
            ;;
        -v|--version)
            VERSION="$2"
            shift 2
            ;;
        -r|--registry)
            REGISTRY="$2"
            shift 2
            ;;
        -h|--help)
            show_help
            exit 0
            ;;
        *)
            log_error "Unknown option: $1"
            show_help
            exit 1
            ;;
    esac
done

# Main execution
case $COMMAND in
    build)
        check_prerequisites
        build_image
        ;;
    deploy)
        check_prerequisites
        build_image
        deploy_docker_compose
        show_status
        ;;
    status)
        show_status
        ;;
    help|"")
        show_help
        ;;
    *)
        log_error "Unknown command: $COMMAND"
        show_help
        exit 1
        ;;
esac

log_success "Script completed successfully"