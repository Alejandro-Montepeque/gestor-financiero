# Gestor Financiero

Personal finance web application: track income, expenses, categories and monthly budgets. Modern rebuild of an academic .NET Framework 4.7.2 project, now on .NET 10 with Clean Architecture, MudBlazor UI, PostgreSQL, and a full production-grade auth stack.

> **Status:** MVP feature-complete. Deployment to Google Cloud Run pending (see roadmap).

---

## Tech stack

### Backend

| Layer            | Technology                                                 |
| ---------------- | ---------------------------------------------------------- |
| Runtime          | .NET 10 (LTS)                                              |
| Web framework    | ASP.NET Core 10 + Blazor Web App (Server + Static SSR)     |
| ORM              | Entity Framework Core 10 + Npgsql provider 10.0.3          |
| Identity         | ASP.NET Core Identity with EF Core stores                  |
| Email            | MailKit 4.16.0 / MimeKit 4.16.0                            |
| Password safety  | Have I Been Pwned API (SHA-1 k-anonymity)                  |
| Rate limiting    | Built-in `System.Threading.RateLimiting`                   |
| Logging          | `Microsoft.Extensions.Logging` + custom `IAppEventLogger`  |

### Frontend

| Layer               | Technology                                    |
| ------------------- | --------------------------------------------- |
| UI framework        | Blazor + MudBlazor 9.8.0                      |
| Interactivity model | Interactive Server (SignalR) + Static SSR     |
| Typography          | Inter (Google Fonts)                          |
| Custom styling      | Hand-crafted CSS on top of MudBlazor          |
| Icons               | Material Icons (via MudBlazor)                |
| Client-side JS      | Vanilla — password strength meter, toggles    |

### Database

| Layer     | Technology                          |
| --------- | ----------------------------------- |
| Engine    | PostgreSQL 17 (Neon serverless)     |
| Access    | EF Core migrations, direct URL only |

### Testing

| Layer            | Technology                              |
| ---------------- | --------------------------------------- |
| Unit tests       | xUnit v2                                |
| Integration      | xUnit + Testcontainers (planned)        |
| Component tests  | bUnit (planned)                         |

### DevOps (planned)

| Layer            | Technology                              |
| ---------------- | --------------------------------------- |
| Container        | Docker multi-stage                      |
| Runtime target   | Google Cloud Run                        |
| Registry         | Google Artifact Registry                |
| CI/CD            | GitHub Actions + Workload Identity Fed. |
| DNS + SSL        | Cloudflare + Cloud Run managed cert     |

---

## Architecture

Clean Architecture across four projects:

```
src/
├── GestorFinanciero.Domain/         Entities, value objects, enums (no dependencies)
├── GestorFinanciero.Application/    DTOs, interfaces, business rules (depends on Domain)
├── GestorFinanciero.Infrastructure/ EF Core, Identity, email, HIBP (depends on Application)
└── GestorFinanciero.Web/            Blazor Server host (depends on Infrastructure)

tests/
├── GestorFinanciero.UnitTests/
└── GestorFinanciero.IntegrationTests/
```

Dependencies point inwards: `Web` → `Infrastructure` → `Application` → `Domain`. The Domain layer has no framework references.

---

## Features

### Auth & security

- Register with email verification (mandatory before login)
- Login with email + password
- Forgot password → email link → reset
- Password strength meter (client-side, no server round-trip)
- Have I Been Pwned check — rejects passwords seen in public breaches
- Account lockout after 5 failed attempts
- IP-based rate limiting (5 login attempts/min, 3 register/5 min)
- Security-stamp revalidation every 30 min
- Audit trail of every auth event in the `app_events` table
- Generic error messages (protection against account enumeration)
- Anti-forgery tokens on every form

### Finance

- Categories: 5 types (Income, Savings, Fixed expense, Variable expense, Debt)
- 5 default categories seeded per user on registration
- Transactions: amount + currency, date, category, description, notes
- Filters by date range and category
- Multi-currency support via `Money` value object

### Dashboard

- Monthly totals: income, expenses, balance, savings
- 5 most recent transactions
- Top 5 expense categories (with progress bars and percentages)
- Month-over-month deltas (with semantic up/down arrows)
- Expense distribution split by type (fixed / variable / debt)
- Health metrics: savings rate ring, avg daily spend, month progress

### Observability

- Global exception middleware writes every unhandled crash to the DB
- Auth events (register/login/logout success and failure) recorded with request context
- Structured logging with named categories

### DX

- Makefile with `artisan`-style commands (see below)
- User Secrets for local configuration
- Fake SMTP mode logs emails to console instead of sending
- Seeder framework tracks execution so each seeder runs once per environment
- Two separate migrations for the initial schema and the unified event log

---

## Getting started

### Prerequisites

- macOS, Linux or Windows
- .NET 10 SDK
- Docker Desktop (for future Postgres via Testcontainers; not required today)
- A Neon PostgreSQL project (free tier) or any Postgres 17

### One-time setup

```bash
git clone git@github.com:Alejandro-Montepeque/gestor-financiero.git
cd gestor-financiero
git checkout dev

# Install global tools + trust local HTTPS cert
make setup

# Restore packages
dotnet restore
```

### Configure the database

1. Create a Neon project (Postgres 17). Copy the **direct** connection URL — NOT the pooled one — from the Neon console.
2. Store it in User Secrets:

   ```bash
   make secrets-set KEY="ConnectionStrings:Default" VALUE="postgresql://user:pass@host/db?sslmode=require"
   ```

3. Apply the initial migrations:

   ```bash
   make migrate
   ```

4. Start the app:

   ```bash
   make dev
   ```

The default seeder creates a demo user on first boot:

- **Email:** `demo@gestor.dev`
- **Password:** `Demo1234`

### Configure email (optional)

By default the SMTP sender runs in **fake mode** — emails are dumped to the console. To enable real delivery via Gmail:

1. Turn on 2-Step Verification for your Google account.
2. Generate an App Password at <https://myaccount.google.com/apppasswords>.
3. Set the secrets:

   ```bash
   make secrets-set KEY="Smtp:UseFakeSender" VALUE="false"
   make secrets-set KEY="Smtp:Host"          VALUE="smtp.gmail.com"
   make secrets-set KEY="Smtp:Port"          VALUE="587"
   make secrets-set KEY="Smtp:Username"      VALUE="your@gmail.com"
   make secrets-set KEY="Smtp:Password"      VALUE="16-char-app-password"
   make secrets-set KEY="Smtp:FromName"      VALUE="Gestor Financiero"
   make secrets-set KEY="Smtp:FromAddress"   VALUE="your@gmail.com"
   ```

---

## Commands

Every workflow ships with a Makefile target — think `php artisan` but for .NET.

### Setup and dev

| Command      | Description                                              |
| ------------ | -------------------------------------------------------- |
| `make setup` | Install `dotnet-ef`, `dotnet-user-secrets`, trust cert   |
| `make build` | Compile the whole solution                               |
| `make dev`   | Build + `dotnet watch` with hot reload                   |
| `make run`   | Run without hot reload (like a production build)         |
| `make test`  | Execute all test projects                                |
| `make check` | `build` + `test` — the "before I commit" target          |
| `make clean` | Delete `bin/` and `obj/` everywhere                      |

### Migrations

| Command                          | Description                                     |
| -------------------------------- | ----------------------------------------------- |
| `make migrate`                   | Apply pending migrations                        |
| `make migrate-list`              | List migrations and their status                |
| `make migrate-new NAME=<name>`   | Create a new migration                          |
| `make migrate-remove`            | Remove the last (unapplied) migration           |
| `make migrate-rollback TO=<id>`  | Revert the DB to a specific migration           |
| `make migrate-reset`             | Revert **every** migration (dangerous, prompts) |

### Seeders

Seeders run automatically at startup and each one is recorded in `seeder_executions` so they never run twice.

| Command                   | Description                                           |
| ------------------------- | ----------------------------------------------------- |
| `make seed-list`          | SQL snippet to list executed seeders on Neon          |
| `make seed-reset-hint`    | Instructions to force a specific seeder to re-run     |

### User secrets

| Command                                              | Description                       |
| ---------------------------------------------------- | --------------------------------- |
| `make secrets`                                       | List all secrets                  |
| `make secrets-set KEY="path" VALUE="value"`          | Set a secret                      |
| `make secrets-clear`                                 | Delete every secret               |

### Deploy (planned)

| Command             | Description                                    |
| ------------------- | ---------------------------------------------- |
| `make docker-build` | Build the Docker image locally                 |
| `make docker-run`   | Run the container against a Postgres via env   |

### Help

| Command      | Description                              |
| ------------ | ---------------------------------------- |
| `make help`  | Print the coloured menu of every command |

---

## Roadmap

- [ ] Session management page (list + revoke active devices)
- [ ] Budgets CRUD (already scaffolded, empty state present)
- [ ] Debts CRUD with amortisation
- [ ] Dashboard chart (Chart.js or ApexCharts)
- [ ] xUnit tests + Testcontainers for integration
- [ ] Docker multi-stage build
- [ ] GitHub Actions CI/CD with Workload Identity Federation
- [ ] Deploy to `gestor.alejandromontepeque.dev`

---

## Credits

Original team version of the project (.NET Framework 4.7.2, SQL Server) built as an academic assignment at **ITCA FEPADE**. This repository holds a personal rewrite on the `dev` branch; the `main` branch preserves the original codebase intact.

Rebuild by **Alejandro Montepeque** — [portfolio](https://alejandromontepeque.dev).
