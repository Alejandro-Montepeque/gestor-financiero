# Gestor Financiero

Personal finance web application: track income, expenses, categories, budgets and debts. Modern rebuild of an academic .NET Framework 4.7.2 project, now on .NET 10 with Clean Architecture, MudBlazor UI, PostgreSQL, and a full production-grade auth stack.

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

- Register with email verification via a 6-digit code (mandatory before login)
- Login with email + password
- Forgot password → email link → reset
- Password strength meter (client-side, no server round-trip)
- Show/hide password toggle on every password field
- Have I Been Pwned check — rejects passwords seen in public breaches
- Account lockout after 5 failed attempts (15-minute cooldown)
- IP-based rate limiting (5 login attempts/min, 3 register/5 min)
- Security-stamp revalidation every 30 min
- Audit trail of every auth event in the `app_events` table
- Generic error messages (protection against account enumeration)
- Anti-forgery tokens on every form
- Security notification emails (password change, email change, account lockout)
- CSP + HSTS + X-Frame-Options + Permissions-Policy headers

### Account management

- Editable profile (full name, preferred currency)
- Change email with confirmation link to the new address
- Change password with old-password verification + HIBP check
- Active session view with IP + user-agent
- "Sign out from all devices" via SecurityStamp rotation
- Delete account (password + confirmation) with EF Core cascade

### Finance

- Categories: 5 types (Income, Savings, Fixed expense, Variable expense, Debt)
- 5 default categories seeded per user on registration
- Transactions: amount + currency, date, category, description, notes
- Filters: date range, category, type, amount range, free-text search
- CSV export respecting the current filters
- Budgets: monthly estimated-vs-actual per category with progress bars
- Debts: register a debt with APR, apply payments with interest auto-computed since the last payment
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
- `.env` file support via dotenv.net + User Secrets for local overrides
- Fake SMTP mode logs emails to console instead of sending
- Seeder framework tracks execution so each seeder runs once per environment

---

## Getting started

### Prerequisites

- macOS, Linux or Windows
- .NET 10 SDK
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

### Configure environment (`.env`)

The `make setup` step above already copied `.env.example` into `.env`. Open it and fill in the real values.

```bash
# Same as running it manually:
make env-init
```

Minimum required keys:

| Variable                     | Example / notes                                            |
| ---------------------------- | ---------------------------------------------------------- |
| `ASPNETCORE_ENVIRONMENT`     | `Development` for local                                    |
| `ConnectionStrings__Default` | Neon direct URL (**not** pooled) — needed for migrations   |
| `Smtp__UseFakeSender`        | `true` = log emails to console; `false` = real SMTP        |
| `Smtp__Host` … `Smtp__FromAddress` | Only needed when `UseFakeSender=false`               |

Notice the **double underscore** in variable names — ASP.NET Core turns that into the config key separator (`:`). So `ConnectionStrings__Default` in the file becomes `configuration["ConnectionStrings:Default"]` in code.

**Configuration precedence** (highest wins):

1. Real environment variables
2. `.env` file (local dev)
3. User Secrets (`dotnet user-secrets`)
4. `appsettings.{Environment}.json`
5. `appsettings.json`

### Configure the database

1. Create a Neon project (Postgres 17). Copy the **direct** connection URL from the Neon console.
2. Set it in your `.env`:

   ```
   ConnectionStrings__Default=postgresql://user:pass@host.neon.tech/db?sslmode=require
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
3. Edit `.env`:

   ```
   Smtp__UseFakeSender=false
   Smtp__Host=smtp.gmail.com
   Smtp__Port=587
   Smtp__Username=your@gmail.com
   Smtp__Password=16-char-app-password
   Smtp__FromName=Gestor Financiero
   Smtp__FromAddress=your@gmail.com
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

### Help

| Command      | Description                              |
| ------------ | ---------------------------------------- |
| `make help`  | Print the coloured menu of every command |

---

## Roadmap

- [x] Auth: register with 6-digit code + login + forgot password + email verification
- [x] Categories, Transactions, Budgets, Debts CRUD
- [x] Dashboard with monthly metrics + comparisons
- [x] Account management (profile, email, password, sessions, delete)
- [x] Security notifications by email
- [x] Responsive layout across all pages
- [ ] xUnit tests + Testcontainers for integration
- [ ] bUnit component tests

---

## Credits

Original team version of the project (.NET Framework 4.7.2, SQL Server) built as an academic assignment at **ITCA FEPADE**. This repository holds a personal rewrite on the `dev` branch; the `main` branch preserves the original codebase intact.

Rebuild by **Alejandro Montepeque** — [portfolio](https://alejandromontepeque.dev).
