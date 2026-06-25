#!/usr/bin/env bash
# Source .env and export variables for compose/Makefile helpers.

_load_env_file() {
  local file="$1"
  [[ -f "${file}" ]] || return 0

  while IFS= read -r line || [[ -n "${line}" ]]; do
    line="${line%$'\r'}"
    [[ -z "${line}" || "${line}" =~ ^[[:space:]]*# ]] && continue
    [[ "${line}" != *"="* ]] && continue

    local key="${line%%=*}"
    local value="${line#*=}"
    key="$(echo "${key}" | xargs)"

    if [[ "${value}" =~ ^\".*\"$ ]]; then
      value="${value:1:${#value}-2}"
    elif [[ "${value}" =~ ^\'.*\'$ ]]; then
      value="${value:1:${#value}-2}"
    fi

    case "${key}" in
      APP_PORT|MAX_CORRECTION_RETRIES)
        printf -v "${key}" '%s' "${value}"
        export "${key}"
        ;;
      OLLAMA_BASE_URL|OLLAMA_MODEL|OPENSCAD_EXECUTABLE|OPENSCAD_REMOTE_URL)
        printf -v "${key}" '%s' "${value}"
        export "${key}"
        ;;
    esac
  done < "${file}"
}

load_env() {
  APP_PORT="${APP_PORT:-8080}"
  OLLAMA_BASE_URL="${OLLAMA_BASE_URL:-http://host.docker.internal:11434}"
  OLLAMA_MODEL="${OLLAMA_MODEL:-llama3.2}"
  OPENSCAD_EXECUTABLE="${OPENSCAD_EXECUTABLE:-openscad}"
  OPENSCAD_REMOTE_URL="${OPENSCAD_REMOTE_URL:-}"
  MAX_CORRECTION_RETRIES="${MAX_CORRECTION_RETRIES:-3}"

  _load_env_file "${ENV_FILE:-.env}"
}

api_url() {
  load_env
  printf 'http://localhost:%s' "${APP_PORT}"
}

is_port_blocked_by_other() {
  local port="$1"
  local own_container_found=false
  local foreign_container_found=false

  if command -v docker >/dev/null 2>&1; then
    while IFS= read -r line; do
      [[ -z "${line}" ]] && continue
      local name="${line%%$'\t'*}"
      local ports="${line#*$'\t'}"
      if [[ "${ports}" == *":${port}->"* ]]; then
        if [[ "${name}" == scad-agent* ]]; then
          own_container_found=true
        else
          foreign_container_found=true
        fi
      fi
    done < <(docker ps --format '{{.Names}}\t{{.Ports}}' 2>/dev/null || true)

    if [[ "${foreign_container_found}" == true ]]; then
      return 0
    fi

    if [[ "${own_container_found}" == true ]]; then
      return 1
    fi
  fi

  if command -v netstat >/dev/null 2>&1; then
    if netstat -ano 2>/dev/null | grep -qE "[:.]${port}[[:space:]]"; then
      return 0
    fi
  fi

  return 1
}

is_port_in_use() {
  is_port_blocked_by_other "$1"
}

find_free_port() {
  local start="${1:-8080}"
  local port="${start}"
  local limit=$((start + 20))
  while (( port <= limit )); do
    if ! is_port_blocked_by_other "${port}"; then
      printf '%s' "${port}"
      return 0
    fi
    port=$((port + 1))
  done
  return 1
}

quote_env_value() {
  local value="$1"
  if [[ "${value}" == *" "* || "${value}" == *"\""* ]]; then
    value="${value//\"/\\\"}"
    printf '"%s"' "${value}"
  else
    printf '%s' "${value}"
  fi
}
