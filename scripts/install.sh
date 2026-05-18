#!/bin/bash
# Krnl-AI CLI Installer
# Usage: curl -fsSL https://krnlai.dev/install.sh | bash
set -euo pipefail

REPO="krnlai/krnlai"
VERSION="${1:-latest}"
INSTALL_DIR="${KrnlAI_HOME:-$HOME/.krnlai}"

echo "========================================"
echo "  Krnl-AI CLI - Installer"
echo "========================================"
echo ""

# Detect OS
OS="$(uname -s)"
ARCH="$(uname -m)"
case "$OS" in
    Linux)   OS="linux" ;;
    Darwin)  OS="macos" ;;
    *)       echo "❌ Unsupported OS: $OS"; exit 1 ;;
esac
echo "📋 Detected: $OS ($ARCH)"

# Check for .NET SDK
if ! command -v dotnet &>/dev/null; then
    echo "📦 .NET SDK not found. Installing..."
    case "$OS" in
        linux)
            curl -sSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
            chmod +x /tmp/dotnet-install.sh
            /tmp/dotnet-install.sh --channel 10.0 --install-dir "$INSTALL_DIR/dotnet"
            export PATH="$INSTALL_DIR/dotnet:$PATH"
            echo "export PATH=\"\$HOME/.krnlai/dotnet:\$PATH\"" >> "$HOME/.bashrc"
            ;;
        macos)
            if command -v brew &>/dev/null; then
                brew install --cask dotnet-sdk
            else
                curl -sSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
                chmod +x /tmp/dotnet-install.sh
                /tmp/dotnet-install.sh --channel 10.0 --install-dir "$INSTALL_DIR/dotnet"
                export PATH="$INSTALL_DIR/dotnet:$PATH"
            fi
            ;;
    esac
    echo "✅ .NET SDK installed"
fi

# Install Krnl-AI CLI
echo "📦 Installing Krnl-AI CLI..."
dotnet tool install -g KrnlAI.Cli 2>/dev/null || dotnet tool update -g KrnlAI.Cli 2>/dev/null || {
    echo "📦 Building from source..."
    TMP_DIR=$(mktemp -d)
    git clone --depth 1 "https://github.com/$REPO.git" "$TMP_DIR"
    cd "$TMP_DIR/src/KrnlAI.Cli"
    dotnet pack -c Release -o /tmp/krnlai-nupkg
    dotnet tool install -g KrnlAI.Cli --add-source /tmp/krnlai-nupkg
    rm -rf "$TMP_DIR"
}

echo ""
echo "✅ Krnl-AI CLI installed successfully!"
echo ""
echo "Next steps:"
echo "  krnlai --help          # View all commands"
echo "  krnlai chat            # Start interactive TUI"
echo "  krnlai new agent demo  # Create a new agent"
echo "  krnlai upgrade         # Check for updates"
echo ""
echo "Documentation: https://opencode.ai"
