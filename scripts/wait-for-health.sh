#!/usr/bin/env bash
set -euo pipefail

API_URL="${1:-http://localhost:8080}"
HEALTH_URL="${API_URL%/}/api/health"
MAX_ATTEMPTS="${MAX_ATTEMPTS:-60}"
SLEEP_SECONDS="${SLEEP_SECONDS:-2}"

if ! command -v curl >/dev/null 2>&1; then
  echo "curl not found; skipping health wait."
  exit 0
fi

echo "Waiting for ${HEALTH_URL} ..."

for ((attempt = 1; attempt <= MAX_ATTEMPTS; attempt++)); do
  if response="$(curl -fsS "${HEALTH_URL}" 2>/dev/null)"; then
    echo "API is healthy."
    if command -v python3 >/dev/null 2>&1; then
      printf '%s' "${response}" | python3 -c "import json,sys; d=json.load(sys.stdin); print('  Ollama reachable:', d.get('ollamaReachable', d.get('OllamaReachable')))" 2>/dev/null || true
    fi
    exit 0
  fi

  if (( attempt == MAX_ATTEMPTS )); then
    echo "Timed out waiting for API health." >&2
    exit 1
  fi

  sleep "${SLEEP_SECONDS}"
done
