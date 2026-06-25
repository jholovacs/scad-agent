.PHONY: setup redeploy build up down logs health migrate config test test-backend test-frontend openscad-host

COMPOSE := docker compose
ENV_FILE := .env
SERVICE := scad-agent
API_URL := $(shell bash -c 'ROOT="$$PWD"; source scripts/lib/env.sh 2>/dev/null; ENV_FILE="$$ROOT/.env"; load_env; api_url')

setup:
	@bash scripts/setup.sh

redeploy: build
	@test -f $(ENV_FILE) || (echo "Run 'make setup' first to create $(ENV_FILE)." && exit 1)
	@bash scripts/check-port.sh
	@echo "Redeploying $(SERVICE) on port $$(bash -c 'source scripts/lib/env.sh; ENV_FILE=.env; load_env; echo $$APP_PORT') (migrations run automatically on startup)..."
	@$(COMPOSE) --env-file $(ENV_FILE) up -d --force-recreate
	@bash scripts/wait-for-health.sh "$(API_URL)"
	@echo "Redeploy complete. App: $(API_URL)"

build:
	@if [ -f $(ENV_FILE) ]; then $(COMPOSE) --env-file $(ENV_FILE) build; else $(COMPOSE) build; fi

up:
	@test -f $(ENV_FILE) || (echo "Run 'make setup' first to create $(ENV_FILE)." && exit 1)
	@bash scripts/check-port.sh
	@$(COMPOSE) --env-file $(ENV_FILE) up -d

down:
	@$(COMPOSE) down

logs:
	@$(COMPOSE) logs -f $(SERVICE)

health:
	@bash scripts/wait-for-health.sh "$(API_URL)"

config:
	@test -f $(ENV_FILE) || (echo "Run 'make setup' first." && exit 1)
	@cat $(ENV_FILE)

migrate:
	@echo "Applying database migrations via container startup..."
	@bash scripts/check-port.sh
	@$(COMPOSE) --env-file $(ENV_FILE) up -d --force-recreate $(SERVICE)
	@bash scripts/wait-for-health.sh "$(API_URL)"

test: test-backend test-frontend
	@echo "All tests passed."

test-backend:
	@echo "Running backend tests..."
	@dotnet test backend/ScadAgent.sln --filter "Category!=OpenScad" --verbosity normal

test-frontend:
	@echo "Running frontend tests..."
	@cd frontend && npm test

openscad-host:
	@bash scripts/openscad-host.sh
