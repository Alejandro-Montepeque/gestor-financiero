# ═══════════════════════════════════════════════════════════════════════
#  Gestor Financiero — dev commands
#  Uso: make <comando>       (ej: make dev, make migrate)
#  Ver todos los comandos:   make help
# ═══════════════════════════════════════════════════════════════════════

.DEFAULT_GOAL := help

# Rutas de proyectos (se usan en todos los comandos)
WEB           := src/GestorFinanciero.Web
INFRA         := src/GestorFinanciero.Infrastructure

# Directorio dentro del proyecto Infrastructure donde viven las migraciones
MIGRATIONS_DIR := Persistence/Migrations

# ═══════════════════════════════════════════════════════════════════════
#  🔧  Setup + herramientas
# ═══════════════════════════════════════════════════════════════════════

setup: ## Instalar dotnet-ef, user-secrets, y trustear el cert HTTPS
	@echo "🔧 Instalando dotnet-ef global tool..."
	@dotnet tool install --global dotnet-ef 2>/dev/null || dotnet tool update --global dotnet-ef
	@echo "🔧 Instalando dotnet-user-secrets..."
	@dotnet tool install --global dotnet-user-secrets 2>/dev/null || dotnet tool update --global dotnet-user-secrets
	@echo "🔒 Trusteando cert HTTPS local..."
	@dotnet dev-certs https --trust
	@$(MAKE) --no-print-directory env-init
	@echo "✅ Setup listo."

env-init: ## Crear .env a partir de .env.example (no sobrescribe si ya existe)
	@if [ -f .env ]; then \
		echo "ℹ️  .env ya existe — no se toca."; \
	else \
		cp .env.example .env; \
		echo "📄 .env creado. Editalo con tus valores (DB, SMTP)."; \
	fi

restore: ## Restaurar packages NuGet
	@dotnet restore

# ═══════════════════════════════════════════════════════════════════════
#  🏗️  Build + run
# ═══════════════════════════════════════════════════════════════════════

build: ## Compilar la solution entera (0 errores para pasar)
	@dotnet build

check: ## Build + tests — falla si algo rojo (útil antes de commit)
	@dotnet build && dotnet test

dev: ## Build + arrancar Blazor con hot reload (=  make build && dotnet watch)
	@dotnet build && dotnet watch --project $(WEB) run

run: ## Arrancar sin hot reload (para probar como en prod)
	@dotnet run --project $(WEB)

test: ## Correr todos los tests
	@dotnet test

clean: ## Borrar bin/ y obj/ de todos los proyectos
	@dotnet clean
	@find . -type d \( -name bin -o -name obj \) -not -path '*/.*' -exec rm -rf {} + 2>/dev/null || true
	@echo "🧹 bin/ y obj/ limpiados."

# ═══════════════════════════════════════════════════════════════════════
#  📦  Migraciones
# ═══════════════════════════════════════════════════════════════════════

migrate: ## Aplicar migraciones pendientes a la DB (Neon)
	@dotnet ef database update --project $(INFRA) --startup-project $(WEB)

migrate-list: ## Listar migraciones y su estado ((Pending) = no aplicada)
	@dotnet ef migrations list --project $(INFRA) --startup-project $(WEB)

migrate-new: ## Crear migración nueva. Uso: make migrate-new NAME=AddPhoneToUser
	@if [ -z "$(NAME)" ]; then \
		echo "❌ Falta NAME. Uso: make migrate-new NAME=AddPhoneToUser"; exit 1; \
	fi
	@dotnet ef migrations add $(NAME) --project $(INFRA) --startup-project $(WEB) --output-dir $(MIGRATIONS_DIR)

migrate-remove: ## Borrar la última migración (solo si NO se aplicó)
	@dotnet ef migrations remove --project $(INFRA) --startup-project $(WEB)

migrate-rollback: ## Revertir hasta una migración. Uso: make migrate-rollback TO=20260811195613_InitialCreate
	@if [ -z "$(TO)" ]; then \
		echo "❌ Falta TO. Uso: make migrate-rollback TO=<MigrationId>"; \
		echo "   Usá 'make migrate-list' para ver los IDs disponibles."; exit 1; \
	fi
	@dotnet ef database update $(TO) --project $(INFRA) --startup-project $(WEB)

migrate-reset: ## ⚠️  Revierte TODAS las migraciones (deja la DB vacía)
	@echo "⚠️  Esto va a borrar todas las tablas del proyecto. Ctrl+C para cancelar, Enter para seguir."
	@read _
	@dotnet ef database update 0 --project $(INFRA) --startup-project $(WEB)

# ═══════════════════════════════════════════════════════════════════════
#  🌱  Seeders (se ejecutan automáticamente al arrancar)
# ═══════════════════════════════════════════════════════════════════════

seed-list: ## Ver qué seeders ya se ejecutaron (query en seeder_executions)
	@echo "📋 Corré este SQL en Neon SQL Editor:"
	@echo ""
	@echo "  SELECT \"Key\", \"ExecutedAt\", \"DurationMs\" FROM seeder_executions ORDER BY \"ExecutedAt\";"
	@echo ""

seed-reset-hint: ## Cómo forzar re-run de un seeder específico
	@echo "🌱 Los seeders se ejecutan una vez y quedan registrados."
	@echo ""
	@echo "Para forzar que se vuelva a ejecutar un seeder:"
	@echo "  1. Andá a Neon SQL Editor"
	@echo "  2. Corré:  DELETE FROM seeder_executions WHERE \"Key\" = '0001_SeedDemoUser';"
	@echo "  3. Rearrancá la app:  make dev"
	@echo ""
	@echo "Para volver a ejecutar TODOS los seeders:"
	@echo "  DELETE FROM seeder_executions;"

# ═══════════════════════════════════════════════════════════════════════
#  🔐  Secrets (user-secrets local)
# ═══════════════════════════════════════════════════════════════════════

secrets: ## Ver todos los user-secrets del proyecto Web
	@dotnet user-secrets list --project $(WEB)

secrets-set: ## Setear un secret. Uso: make secrets-set KEY="ConnectionStrings:Default" VALUE="postgres://..."
	@if [ -z "$(KEY)" ] || [ -z "$(VALUE)" ]; then \
		echo "❌ Faltan KEY y/o VALUE. Uso: make secrets-set KEY=... VALUE=..."; exit 1; \
	fi
	@dotnet user-secrets set "$(KEY)" "$(VALUE)" --project $(WEB)

secrets-clear: ## Borrar TODOS los user-secrets (⚠️ irreversible en la sesión)
	@dotnet user-secrets clear --project $(WEB)

# ═══════════════════════════════════════════════════════════════════════
#  🚀  Deploy shortcuts (después que agreguemos Docker + CI)
# ═══════════════════════════════════════════════════════════════════════

docker-build: ## Build de la imagen Docker localmente
	@docker build -t gestor-financiero:local .

docker-run: ## Correr el container localmente (require ConnectionStrings__Default env)
	@docker run --rm -p 8080:8080 \
		-e "ConnectionStrings__Default=$$CONNECTION_STRING" \
		gestor-financiero:local

# ═══════════════════════════════════════════════════════════════════════
#  📖  Help
# ═══════════════════════════════════════════════════════════════════════

help: ## Mostrar esta ayuda
	@echo ""
	@echo "  Gestor Financiero — comandos disponibles"
	@echo "  ────────────────────────────────────────"
	@echo ""
	@awk 'BEGIN {FS = ":.*?## "} \
		/^[a-zA-Z_-]+:.*?## / { \
			printf "  \033[36m%-22s\033[0m %s\n", $$1, $$2 \
		} \
		/^# ═+/ { next } \
		/^#  [🔧🏗️📦🌱🔐🚀📖]/ { printf "\n\033[33m%s\033[0m\n", substr($$0, 3) }' \
		$(MAKEFILE_LIST)
	@echo ""

.PHONY: help setup env-init restore build check dev run test clean \
	migrate migrate-list migrate-new migrate-remove migrate-rollback migrate-reset \
	seed-list seed-reset-hint \
	secrets secrets-set secrets-clear \
	docker-build docker-run
