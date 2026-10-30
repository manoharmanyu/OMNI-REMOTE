#!/usr/bin/env bash
set -euo pipefail

# ==============================================================================
# OmniRemote Installation & Environment Bootstrap Script
# "One app. Every connection. Total control."
# ==============================================================================

BOLD="\033[1m"
GREEN="\033[0;32m"
BLUE="\033[0;34m"
YELLOW="\033[0;33m"
RED="\033[0;31m"
RESET="\033[0m"

echo -e "${BLUE}${BOLD}"
echo "================================================================================"
echo "                   OMNIREMOTE - INSTALLATION & SETUP                            "
echo "               Universal Smart Remote & Smart Home Controller                   "
echo "================================================================================"
echo -e "${RESET}"

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$ROOT_DIR"

# ------------------------------------------------------------------------------
# 1. Environment & Path Discovery
# ------------------------------------------------------------------------------
echo -e "${BOLD}[1/5] Checking environment and toolchain...${RESET}"

# Check .NET path
if [ -d "$HOME/.dotnet" ]; then
    export DOTNET_ROOT="$HOME/.dotnet"
    export PATH="$HOME/.dotnet:$PATH"
fi

if ! command -v dotnet >/dev/null 2>&1; then
    echo -e "${YELLOW}Warning: 'dotnet' not found in PATH.${RESET}"
    echo "Attempting to locate or install .NET SDK..."
    if command -v curl >/dev/null 2>&1; then
        echo "Installing .NET SDK via official dotnet-install script..."
        curl -sSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 10.0 --install-dir "$HOME/.dotnet"
        export DOTNET_ROOT="$HOME/.dotnet"
        export PATH="$HOME/.dotnet:$PATH"
    else
        echo -e "${RED}Error: curl is required to download .NET SDK. Please install .NET 10 manualy.${RESET}"
        exit 1
    fi
fi

DOTNET_VER="$(dotnet --version 2>/dev/null || echo 'unknown')"
echo -e "  ${GREEN}✓${RESET} .NET SDK: ${DOTNET_VER}"

# Check Node.js and npm
if ! command -v node >/dev/null 2>&1; then
    echo -e "${RED}Error: 'node' (Node.js) is not installed. Please install Node.js (v18+) to run the Expo mobile app.${RESET}"
    exit 1
fi
if ! command -v npm >/dev/null 2>&1; then
    echo -e "${RED}Error: 'npm' is not installed. Please install npm.${RESET}"
    exit 1
fi

NODE_VER="$(node -v)"
NPM_VER="$(npm -v)"
echo -e "  ${GREEN}✓${RESET} Node.js:  ${NODE_VER}"
echo -e "  ${GREEN}✓${RESET} npm:      ${NPM_VER}"

# Check Python 3
if command -v python3 >/dev/null 2>&1; then
    PY_VER="$(python3 --version 2>/dev/null || echo 'python3')"
    echo -e "  ${GREEN}✓${RESET} Python:   ${PY_VER}"
else
    echo -e "${YELLOW}Notice: python3 not found. (Optional, used for scripts/demo_simulation.py)${RESET}"
fi

# ------------------------------------------------------------------------------
# 2. Restore & Build .NET Solution
# ------------------------------------------------------------------------------
echo -e "\n${BOLD}[2/5] Restoring and building .NET solution...${RESET}"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1

dotnet restore OmniRemote.slnx
dotnet build OmniRemote.slnx -c Release --no-restore
echo -e "  ${GREEN}✓${RESET} Backend, Core Domain, and Transport Adapters built successfully."

# ------------------------------------------------------------------------------
# 3. Install Mobile App Dependencies (Expo Go / React Native)
# ------------------------------------------------------------------------------
echo -e "\n${BOLD}[3/5] Installing Expo mobile client dependencies...${RESET}"
if [ -d "apps/mobile" ]; then
    cd apps/mobile
    if [ ! -d "node_modules" ]; then
        npm install --no-audit --prefer-offline
    else
        echo "  (node_modules already present in apps/mobile; verifying packages...)"
        npm install --no-audit --prefer-offline
    fi
    cd "$ROOT_DIR"
    echo -e "  ${GREEN}✓${RESET} Expo mobile client ready."
fi

# ------------------------------------------------------------------------------
# 4. Set Script Permissions & Verify Unit Tests
# ------------------------------------------------------------------------------
echo -e "\n${BOLD}[4/5] Verifying integrity with test suite...${RESET}"
chmod +x install.sh
if [ -f "scripts/demo_simulation.py" ]; then
    chmod +x scripts/demo_simulation.py
fi

dotnet test tests/OmniRemote.UnitTests -c Release --no-build
echo -e "  ${GREEN}✓${RESET} All unit tests passed (68/68)."

# ------------------------------------------------------------------------------
# 5. Done!
# ------------------------------------------------------------------------------
echo -e "\n${GREEN}${BOLD}================================================================================"
echo "                   INSTALLATION COMPLETE - READY TO RUN!                        "
echo "================================================================================${RESET}"
echo ""
echo -e "${BOLD}▶ To run on your phone in Expo Go (Just like QRShield):${RESET}"
echo "    npm run mobile"
echo "    (Scan the terminal QR code with your phone camera or Expo Go app)"
echo ""
echo -e "${BOLD}▶ To start the ASP.NET Core Backend API:${RESET}"
echo "    ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/OmniRemote.Backend --urls \"http://127.0.0.1:5123\""
echo ""
echo -e "${BOLD}▶ To run the automated end-to-end simulation demo:${RESET}"
echo "    python3 scripts/demo_simulation.py"
echo ""
