#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
# shellcheck source=lib/env.sh
source "${ROOT_DIR}/scripts/lib/env.sh"

ENV_FILE="${ROOT_DIR}/.env"
load_env

if is_port_blocked_by_other "${APP_PORT}"; then
  echo "Error: host port ${APP_PORT} is already in use by another process." >&2
  echo "" >&2
  if command -v docker >/dev/null 2>&1; then
    echo "Containers publishing that port:" >&2
    docker ps --format 'table {{.Names}}\t{{.Ports}}\t{{.Status}}' 2>/dev/null \
      | grep -E "PORTS|:${APP_PORT}->" || true
    echo "" >&2
  fi
  SUGGESTED="$(find_free_port "$((APP_PORT + 1))" || true)"
  if [[ -n "${SUGGESTED}" ]]; then
    echo "Set a different port in .env, for example:" >&2
    echo "  APP_PORT=${SUGGESTED}" >&2
  fi
  echo "" >&2
  echo "Or stop the process using port ${APP_PORT}, then run make redeploy again." >&2
  exit 1
fi
