#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
CONFIG="$REPO_DIR/cg-broker.yaml"

echo "=== cg-mqtt-broker: install ==="

# 1) Install mosquitto
echo "[1/5] Installing mosquitto and mosquitto-clients..."
apt update
apt install -y mosquitto mosquitto-clients

# 2) Enable and start mosquitto
echo "[2/5] Enabling mosquitto service..."
systemctl enable --now mosquitto

# 3) Build cg-mosqctl
echo "[3/5] Building cg-mosqctl..."
if ! command -v dotnet &>/dev/null; then
    echo "[ERROR] .NET SDK not found. Install .NET 8 SDK first:"
    echo "        https://learn.microsoft.com/en-us/dotnet/core/install/linux-ubuntu"
    exit 1
fi

dotnet publish "$REPO_DIR/src/CgMosqCtl/CgMosqCtl.csproj" \
    -c Release \
    -o /usr/local/lib/cg-mosqctl \
    --self-contained false

ln -sf /usr/local/lib/cg-mosqctl/cg-mosqctl /usr/local/bin/cg-mosqctl
echo "[INFO] cg-mosqctl installed to /usr/local/bin/cg-mosqctl"

# 4) Apply config
echo "[4/5] Applying config..."
if [ ! -f "$CONFIG" ]; then
    echo "[WARN] $CONFIG not found, copying example..."
    cp "$REPO_DIR/cg-broker.example.yaml" "$CONFIG"
fi

cg-mosqctl apply --config "$CONFIG"

# 5) Smoke test
echo "[5/5] Smoke test..."
sleep 1
"$SCRIPT_DIR/status.sh"

echo ""
echo "=== Installation complete ==="
echo "Config: $CONFIG"
echo "Mosquitto conf: /etc/mosquitto/conf.d/cg.conf"
echo "Logs: /var/log/mosquitto/mosquitto.log"
