# SCAD Agent

An OpenSCAD design agent with a C# Web API backend, React visualization frontend, and external Ollama LLM integration. The API orchestrates prompt context, OpenSCAD rendering, correction loops, and session persistence in SQLite.

## Architecture

- **ScadAgent.Domain** — entities and value objects
- **ScadAgent.Application** — agent orchestration, context management, ports
- **ScadAgent.Infrastructure** — EF Core SQLite, Ollama HTTP client, OpenSCAD CLI runner, artifact storage
- **ScadAgent.Api** — REST API, SignalR hub, static SPA hosting
- **frontend** — React + Vite workspace with 3D STL preview (React Three Fiber)

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- [Node.js 22+](https://nodejs.org/)
- [OpenSCAD](https://openscad.org/) (for local rendering)
- [Ollama](https://ollama.com/) running locally with a model (default: `llama3.2`)

## Local development

### 1. Start Ollama

```bash
ollama pull llama3.2
ollama serve
```

### 2. Run the API

```bash
cd backend
dotnet run --project ScadAgent.Api
```

API: http://localhost:5080

### 3. Run the frontend

```bash
cd frontend
npm install
npm run dev
```

UI: http://localhost:5173 (proxies `/api` and `/hubs` to the API)

## Docker

### Quick start (recommended)

Interactive setup — configures Ollama URL, model, and OpenSCAD executable path, then builds and starts the stack:

```bash
make setup
```

After code changes, rebuild and redeploy (database migrations run automatically on container startup):

```bash
make redeploy
```

Other targets:

| Command | Description |
|---------|-------------|
| `make build` | Build the Docker image |
| `make up` | Start containers |
| `make down` | Stop containers |
| `make logs` | Follow container logs |
| `make health` | Wait for `/api/health` |
| `make test` | Run backend and frontend tests |
| `make openscad-host` | Run OpenSCAD on the Windows/Linux host (for remote rendering) |

App: http://localhost:8080

Data (SQLite + artifacts) is stored in the `scad-agent-data` volume.

### OpenSCAD: Docker vs host

A Linux Docker container **cannot** run `C:\Program Files\OpenSCAD\openscad.exe`. Choose one:

| Mode | When to use | Setup |
|------|-------------|-------|
| **Container OpenSCAD** (default) | Simplest Docker deployment | Leave `OPENSCAD_REMOTE_URL` empty; the image includes `openscad` |
| **Host OpenSCAD** | Use your Windows install from Docker | Set `OPENSCAD_EXECUTABLE` in `.env`, run `make openscad-host`, set `OPENSCAD_REMOTE_URL=http://host.docker.internal:9333`, then `make redeploy` |
| **Local dev** | API on Windows without Docker | `dotnet run` in `backend/ScadAgent.Api` with `OPENSCAD_EXECUTABLE` set |

### Manual Docker Compose

```bash
cp .env.example .env
docker compose up --build
```

## Configuration

| Setting | Environment variable | Default |
|---------|---------------------|---------|
| Ollama URL | `OLLAMA_BASE_URL` / `Ollama__BaseUrl` | `http://host.docker.internal:11434` |
| Ollama model | `OLLAMA_MODEL` / `Ollama__Model` | `llama3.2` |
| OpenSCAD executable (host/local) | `OPENSCAD_EXECUTABLE` | `openscad` |
| OpenSCAD remote URL (Docker → host) | `OPENSCAD_REMOTE_URL` / `Agent__OpenScadRemoteUrl` | _(empty — use container OpenSCAD)_ |
| SQLite path | `Storage__DatabasePath` | `data/scad-agent.db` |
| Artifacts path | `Storage__ArtifactsPath` | `data/artifacts` |
| Max correction retries | `MAX_CORRECTION_RETRIES` / `Agent__MaxCorrectionRetries` | `3` |
| Host port | `APP_PORT` | `8080` |

## API endpoints

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/health` | API, Ollama, and OpenSCAD health |
| GET/POST | `/api/sessions` | List / create sessions |
| GET | `/api/sessions/{id}` | Session detail |
| POST | `/api/sessions/{id}/messages` | Send instruction (runs agent) |
| GET | `/api/sessions/{id}/iterations` | Iteration history |
| GET | `/api/iterations/{id}/artifacts/stl` | Download STL |
| GET | `/api/iterations/{id}/artifacts/preview` | PNG preview |
| WS | `/hubs/agent` | SignalR progress events |

## Tests

```bash
make test
```

Backend only (skips OpenSCAD integration tests when the CLI is not installed):

```bash
make test-backend
# or: dotnet test backend/ScadAgent.sln --filter "Category!=OpenScad"
```

Include OpenSCAD tests when the CLI is installed:

```bash
dotnet test backend/ScadAgent.sln
```

Frontend only:

```bash
make test-frontend
# or: cd frontend && npm test
```

## License

MIT
