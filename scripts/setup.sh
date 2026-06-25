#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
ENV_FILE="${ROOT_DIR}/.env"
ENV_EXAMPLE="${ROOT_DIR}/.env.example"

cd "${ROOT_DIR}"

if ! command -v docker >/dev/null 2>&1; then
  echo "Error: docker is not installed or not on PATH." >&2
  exit 1
fi

if ! docker compose version >/dev/null 2>&1; then
  echo "Error: docker compose is not available." >&2
  exit 1
fi

prompt() {
  local var_name="$1"
  local prompt_text="$2"
  local default_value="$3"
  local input

  read -r -p "${prompt_text} [${default_value}]: " input
  if [[ -z "${input}" ]]; then
    printf '%s' "${default_value}"
  else
    printf '%s' "${input}"
  fi
}

detect_openscad() {
  if command -v openscad >/dev/null 2>&1; then
    command -v openscad
    return
  fi

  local candidates=(
    "/usr/bin/openscad"
    "/usr/local/bin/openscad"
    "/c/Program Files/OpenSCAD/openscad.exe"
    "/mnt/c/Program Files/OpenSCAD/openscad.exe"
  )

  for candidate in "${candidates[@]}"; do
    if [[ -f "${candidate}" ]]; then
      printf '%s' "${candidate}"
      return
    fi
  done

  printf '%s' "openscad"
}

load_existing_env() {
  OLLAMA_BASE_URL=""
  OLLAMA_MODEL=""
  OPENSCAD_EXECUTABLE=""
  OPENSCAD_REMOTE_URL=""
  MAX_CORRECTION_RETRIES=""
  APP_PORT=""

  _load_env_file "${ENV_FILE}"
}

# shellcheck source=lib/env.sh
source "${ROOT_DIR}/scripts/lib/env.sh"

echo "SCAD Agent setup"
echo "================"
echo ""
echo "Configure Ollama and OpenSCAD, then build and start the Docker stack."
echo ""
echo "OpenSCAD deployment:"
echo "  • Docker (default): OpenSCAD runs inside the container — leave OPENSCAD_REMOTE_URL empty."
echo "  • Host OpenSCAD: run 'make openscad-host' in another terminal and set"
echo "    OPENSCAD_REMOTE_URL=http://host.docker.internal:9333 in .env"
echo ""

load_existing_env

DEFAULT_OLLAMA_URL="${OLLAMA_BASE_URL:-http://host.docker.internal:11434}"
DEFAULT_OLLAMA_MODEL="${OLLAMA_MODEL:-llama3.2}"
DEFAULT_OPENSCAD="${OPENSCAD_EXECUTABLE:-openscad}"
DEFAULT_RETRIES="${MAX_CORRECTION_RETRIES:-3}"
DEFAULT_PORT="${APP_PORT:-8080}"

if is_port_blocked_by_other "${DEFAULT_PORT}"; then
  SUGGESTED="$(find_free_port "$((DEFAULT_PORT + 1))" || echo 8081)"
  echo "Note: port ${DEFAULT_PORT} is already in use (e.g. another Docker container)."
  echo "Defaulting suggested host port to ${SUGGESTED}."
  DEFAULT_PORT="${SUGGESTED}"
fi

if [[ "${DEFAULT_OPENSCAD}" == "openscad" ]]; then
  DETECTED_OPENSCAD="$(detect_openscad)"
  if [[ "${DETECTED_OPENSCAD}" != "openscad" ]]; then
  echo "Detected OpenSCAD at: ${DETECTED_OPENSCAD}"
  echo "Docker deployments typically use 'openscad' (installed in the container)."
  fi
fi

OLLAMA_BASE_URL="$(prompt OLLAMA_BASE_URL "Ollama server URL" "${DEFAULT_OLLAMA_URL}")"
OLLAMA_MODEL="$(prompt OLLAMA_MODEL "Ollama model" "${DEFAULT_OLLAMA_MODEL}")"
OPENSCAD_EXECUTABLE="$(prompt OPENSCAD_EXECUTABLE "Host OpenSCAD path (for make openscad-host / local dev)" "${DEFAULT_OPENSCAD}")"
DEFAULT_REMOTE_URL="${OPENSCAD_REMOTE_URL:-}"
OPENSCAD_REMOTE_URL="$(prompt OPENSCAD_REMOTE_URL "Remote OpenSCAD URL for Docker (blank = use container OpenSCAD)" "${DEFAULT_REMOTE_URL}")"
MAX_CORRECTION_RETRIES="$(prompt MAX_CORRECTION_RETRIES "Max render correction retries" "${DEFAULT_RETRIES}")"
APP_PORT="$(prompt APP_PORT "Host port for the web app" "${DEFAULT_PORT}")"

while is_port_blocked_by_other "${APP_PORT}"; do
  echo "Port ${APP_PORT} is in use. Choose another."
  SUGGESTED="$(find_free_port "$((APP_PORT + 1))" || echo $((APP_PORT + 1)))"
  APP_PORT="$(prompt APP_PORT "Host port for the web app" "${SUGGESTED}")"
done

cat > "${ENV_FILE}" <<EOF
OLLAMA_BASE_URL=${OLLAMA_BASE_URL}
OLLAMA_MODEL=${OLLAMA_MODEL}
OPENSCAD_EXECUTABLE=$(quote_env_value "${OPENSCAD_EXECUTABLE}")
OPENSCAD_REMOTE_URL=${OPENSCAD_REMOTE_URL}
MAX_CORRECTION_RETRIES=${MAX_CORRECTION_RETRIES}
APP_PORT=${APP_PORT}
EOF

echo ""
echo "Wrote ${ENV_FILE}"
echo ""
echo "Building Docker image..."
docker compose --env-file "${ENV_FILE}" build

echo ""
echo "Starting services..."
docker compose --env-file "${ENV_FILE}" up -d

echo ""
bash "${ROOT_DIR}/scripts/wait-for-health.sh" "$(api_url)"

echo ""
echo "Setup complete."
echo "  App:    $(api_url)"
echo "  Health: $(api_url)/api/health"
echo ""
echo "Useful commands:"
echo "  make redeploy  # rebuild and restart after code changes"
echo "  make logs      # follow container logs"
echo "  make down      # stop the stack"
