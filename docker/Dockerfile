# Build frontend
FROM node:22-alpine AS frontend-build
WORKDIR /app/frontend
COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci
COPY frontend/ ./
RUN npm run build

# Publish API
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS api-publish
WORKDIR /app
COPY backend/ ./backend/
RUN dotnet publish backend/ScadAgent.Api/ScadAgent.Api.csproj -c Release -o /app/publish

# Runtime with OpenSCAD
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends openscad curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=api-publish /app/publish ./
COPY --from=frontend-build /app/frontend/dist ./wwwroot

ENV ASPNETCORE_URLS=http://+:8080
ENV Storage__DatabasePath=/data/scad-agent.db
ENV Storage__ArtifactsPath=/data/artifacts
ENV Ollama__BaseUrl=http://host.docker.internal:11434
ENV Ollama__Model=llama3.2
ENV Agent__MaxCorrectionRetries=3
ENV Agent__OpenScadExecutablePath=openscad

EXPOSE 8080
VOLUME ["/data"]

HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
  CMD curl -f http://localhost:8080/api/health || exit 1

ENTRYPOINT ["dotnet", "ScadAgent.Api.dll"]
