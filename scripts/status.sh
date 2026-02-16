#!/usr/bin/env bash
set -euo pipefail

echo "=== cg-mqtt-broker: status ==="

# 1) Mosquitto service status
echo ""
echo "--- systemctl status mosquitto ---"
systemctl status mosquitto --no-pager || true

# 2) Listening ports
echo ""
echo "--- Listening port (mosquitto) ---"
ss -tlnp | grep mosquitto || echo "(mosquitto not found in listening ports)"

# 3) Last log lines
LOG="/var/log/mosquitto/mosquitto.log"
echo ""
echo "--- Last 15 lines of $LOG ---"
if [ -f "$LOG" ]; then
    tail -n 15 "$LOG"
else
    echo "(log file not found: $LOG)"
fi

# 4) Quick smoke test
echo ""
echo "--- Smoke test ---"
BIND_IP="10.10.10.1"
TOPIC="cg/v1/telemetry/SN/SMOKE_TEST"
MSG="smoke_$(date +%s)"

# Subscribe in background, wait for 1 message, timeout 5s
RESULT=$(timeout 5 mosquitto_sub -h "$BIND_IP" -t "$TOPIC" -C 1 &
    SUB_PID=$!
    sleep 1
    mosquitto_pub -h "$BIND_IP" -t "$TOPIC" -m "$MSG"
    wait $SUB_PID 2>/dev/null
) || true

if echo "$RESULT" | grep -q "$MSG"; then
    echo "[OK] Smoke test passed: published and received '$MSG'"
else
    echo "[WARN] Smoke test inconclusive (broker may not be bound to $BIND_IP on this machine)"
    echo "       Try manually:"
    echo "         mosquitto_sub -h $BIND_IP -t '$TOPIC' -v"
    echo "         mosquitto_pub -h $BIND_IP -t '$TOPIC' -m 'hello'"
fi

echo ""
echo "=== Status check complete ==="
