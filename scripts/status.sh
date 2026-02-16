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

# 4) Quick smoke test — telemetry topic
echo ""
echo "--- Smoke test: cg/v1/telemetry ---"
BIND_IP="10.10.10.1"
TOPIC_RAW="cg/v1/telemetry/SN/SMOKE_TEST"
MSG_RAW="smoke_raw_$(date +%s)"

RESULT_RAW=$(timeout 5 mosquitto_sub -h "$BIND_IP" -t "$TOPIC_RAW" -C 1 &
    SUB_PID=$!
    sleep 1
    mosquitto_pub -h "$BIND_IP" -t "$TOPIC_RAW" -m "$MSG_RAW"
    wait $SUB_PID 2>/dev/null
) || true

if echo "$RESULT_RAW" | grep -q "$MSG_RAW"; then
    echo "[OK] telemetry smoke test passed: '$MSG_RAW'"
else
    echo "[WARN] telemetry smoke test inconclusive"
    echo "       mosquitto_sub -h $BIND_IP -t '$TOPIC_RAW' -v"
    echo "       mosquitto_pub -h $BIND_IP -t '$TOPIC_RAW' -m 'hello'"
fi

# 5) Quick smoke test — decoded topic
echo ""
echo "--- Smoke test: cg/v1/decoded ---"
TOPIC_DEC="cg/v1/decoded/SN/SMOKE_TEST"
MSG_DEC="smoke_dec_$(date +%s)"

RESULT_DEC=$(timeout 5 mosquitto_sub -h "$BIND_IP" -t "$TOPIC_DEC" -C 1 &
    SUB_PID=$!
    sleep 1
    mosquitto_pub -h "$BIND_IP" -t "$TOPIC_DEC" -m "$MSG_DEC"
    wait $SUB_PID 2>/dev/null
) || true

if echo "$RESULT_DEC" | grep -q "$MSG_DEC"; then
    echo "[OK] decoded smoke test passed: '$MSG_DEC'"
else
    echo "[WARN] decoded smoke test inconclusive"
    echo "       mosquitto_sub -h $BIND_IP -t '$TOPIC_DEC' -v"
    echo "       mosquitto_pub -h $BIND_IP -t '$TOPIC_DEC' -m 'hello'"
fi

echo ""
echo "=== Status check complete ==="
