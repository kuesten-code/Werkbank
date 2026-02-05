#!/bin/bash

# =============================================================================
# Küstencode Werkbank - Update Script
# =============================================================================
# Prüft auf neue Docker Images und aktualisiert die Container
#
# Usage: ./update-werkbank.sh [options]
#   -f, --force     Force restart even if no changes detected
#   -c, --check     Check/pull only, don't restart containers
#   -n, --new       Also check for new modules
#   -h, --help      Show this help
# =============================================================================

set -e

# Configuration
DOCKER_USER="${DOCKERHUB_USERNAME:-kuestencode}"
COMPOSE_FILE="${COMPOSE_FILE:-docker-compose.yml}"

# Known modules
KNOWN_MODULES=("host" "faktura" "rapport" "offerte" "acta")

# Potential new modules
POTENTIAL_MODULES=("lager" "buchhaltung" "projekt" "kunde" "dokument")

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
GRAY='\033[0;90m'
NC='\033[0m' # No Color

# Parse arguments
FORCE=false
CHECK_ONLY=false
CHECK_NEW=false

while [[ $# -gt 0 ]]; do
    case $1 in
        -f|--force)
            FORCE=true
            shift
            ;;
        -c|--check)
            CHECK_ONLY=true
            shift
            ;;
        -n|--new)
            CHECK_NEW=true
            shift
            ;;
        -h|--help)
            head -20 "$0" | tail -15
            exit 0
            ;;
        *)
            echo -e "${RED}Unknown option: $1${NC}"
            exit 1
            ;;
    esac
done

echo -e "${BLUE}╔════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║     Küstencode Werkbank Update Script      ║${NC}"
echo -e "${BLUE}╚════════════════════════════════════════════╝${NC}"
echo ""

# Function to get local image ID
get_local_id() {
    local image=$1
    docker images --format "{{.ID}}" "$image" 2>/dev/null | head -1
}

# Function to check if image exists on Docker Hub
image_exists() {
    local image=$1
    docker manifest inspect "$image" &>/dev/null
}

# Check for updates
echo -e "${YELLOW}Checking for updates...${NC}"
echo ""

UPDATES_AVAILABLE=()

for module in "${KNOWN_MODULES[@]}"; do
    image="${DOCKER_USER}/${module}:latest"

    printf "  %-12s " "$module"

    # Check if image exists remotely
    if ! image_exists "$image"; then
        echo -e "${RED}not found on Docker Hub${NC}"
        continue
    fi

    # Get local ID before pull
    local_id_before=$(get_local_id "$image")

    # Pull the image
    docker pull "$image" --quiet >/dev/null 2>&1

    # Get local ID after pull
    local_id_after=$(get_local_id "$image")

    # Compare
    if [ -z "$local_id_before" ]; then
        echo -e "${GREEN}downloaded (new)${NC}"
        UPDATES_AVAILABLE+=("$module")
    elif [ "$local_id_before" != "$local_id_after" ]; then
        echo -e "${GREEN}updated${NC}"
        UPDATES_AVAILABLE+=("$module")
    else
        echo -e "${GRAY}up to date${NC}"
    fi
done

echo ""

# Check for new modules
if [ "$CHECK_NEW" = true ]; then
    echo -e "${YELLOW}Checking for new modules...${NC}"
    echo ""

    NEW_MODULES=()
    for module in "${POTENTIAL_MODULES[@]}"; do
        image="${DOCKER_USER}/${module}:latest"
        printf "  %-12s " "$module"

        if image_exists "$image"; then
            echo -e "${GREEN}available (new!)${NC}"
            NEW_MODULES+=("$module")
        else
            echo -e "${GRAY}not available${NC}"
        fi
    done

    echo ""

    if [ ${#NEW_MODULES[@]} -gt 0 ]; then
        echo -e "${GREEN}New modules found: ${NEW_MODULES[*]}${NC}"
        echo -e "${YELLOW}Add them to your docker-compose.yml to use them.${NC}"
        echo ""
    fi
fi

# Summary
if [ ${#UPDATES_AVAILABLE[@]} -eq 0 ]; then
    echo -e "${GREEN}All modules are up to date!${NC}"
    echo ""

    if [ "$FORCE" = false ]; then
        exit 0
    else
        echo -e "${YELLOW}Force mode: restarting containers anyway...${NC}"
    fi
else
    echo -e "${YELLOW}Updates pulled for: ${UPDATES_AVAILABLE[*]}${NC}"
fi

echo ""

# Exit if check only
if [ "$CHECK_ONLY" = true ]; then
    echo -e "${BLUE}Check complete. Images have been pulled but containers not restarted.${NC}"
    echo -e "${BLUE}Run without -c to restart containers with new images.${NC}"
    exit 0
fi

# Check if compose file exists
if [ ! -f "$COMPOSE_FILE" ]; then
    echo -e "${RED}Compose file not found: $COMPOSE_FILE${NC}"
    echo -e "${YELLOW}Please restart your containers manually with:${NC}"
    echo "  docker-compose up -d"
    exit 1
fi

echo -e "${YELLOW}Restarting containers...${NC}"

# Restart with docker-compose
docker-compose -f "$COMPOSE_FILE" up -d

echo ""
echo -e "${GREEN}╔════════════════════════════════════════════╗${NC}"
echo -e "${GREEN}║           Update complete!                 ║${NC}"
echo -e "${GREEN}╚════════════════════════════════════════════╝${NC}"
echo ""

# Show running containers
echo -e "${BLUE}Running containers:${NC}"
docker-compose -f "$COMPOSE_FILE" ps

echo ""
echo -e "${YELLOW}Tip: Check the logs with: docker-compose logs -f${NC}"
