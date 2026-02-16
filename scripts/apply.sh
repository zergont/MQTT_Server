#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
CONFIG="$REPO_DIR/cg-broker.yaml"

echo "=== cg-mqtt-broker: apply ==="

# Pull latest changes (if in a git repo)
if [ -d "$REPO_DIR/.git" ]; then
    echo "[INFO] Pulling latest changes from git..."
    cd "$REPO_DIR"
    git pull --ff-only || echo "[WARN] git pull failed, continuing with current files"
fi

# Validate and apply
echo "[INFO] Applying config: $CONFIG"
cg-mosqctl apply --config "$CONFIG"

# Show status
echo ""
systemctl status mosquitto --no-pager || true

echo ""
echo "=== Apply complete ==="
