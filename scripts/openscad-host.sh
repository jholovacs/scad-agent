#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
ENV_FILE="${ROOT_DIR}/.env"

# shellcheck source=lib/env.sh
source "${ROOT_DIR}/scripts/lib/env.sh"
load_env

PORT="${OPENSCAD_HOST_PORT:-9333}"

echo "Starting host OpenSCAD service on http://localhost:${PORT}"
echo "  Executable: ${OPENSCAD_EXECUTABLE}"
echo ""
echo "Point Docker at this service with:"
echo "  OPENSCAD_REMOTE_URL=http://host.docker.internal:${PORT}"
echo ""

cd "${ROOT_DIR}"
OPENSCAD_EXECUTABLE="${OPENSCAD_EXECUTABLE}" PORT="${PORT}" \
  dotnet run --project host/ScadAgent.OpenScadHost/ScadAgent.OpenScadHost.csproj
