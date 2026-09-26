.PHONY: help up down build rebuild restart logs ps shell restore test clean

help:
	@echo "KitchenOS SCADA"
	@echo ""
	@echo "make up       - Start development environment"
	@echo "make down     - Stop containers"
	@echo "make build    - Build Docker images"
	@echo "make rebuild  - Rebuild images without cache"
	@echo "make restart  - Restart containers"
	@echo "make logs     - Follow backend logs"
	@echo "make ps       - Show running containers"
	@echo "make shell    - Open shell inside backend container"
	@echo "make restore  - Restore .NET dependencies"
	@echo "make test     - Run .NET tests"
	@echo "make clean    - Stop containers and remove volumes"

up:
	docker compose up

down:
	docker compose down

build:
	docker compose build

rebuild:
	docker compose build --no-cache

restart:
	docker compose restart

logs:
	docker compose logs -f backend

ps:
	docker compose ps

shell:
	docker compose exec backend /bin/bash

restore:
	docker compose exec backend dotnet restore

test:
	docker compose exec backend dotnet test

clean:
	docker compose down --volumes --remove-orphans