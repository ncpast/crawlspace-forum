# Crawlspace Forum

Imageboard-style forum. University team project.

| Part | Tech | Where it runs |
|---|---|---|
| Frontend | React (Vite) | browser |
| Backend | .NET 10 API | port 8000 |
| Database | PostgreSQL 17 | Docker, port 5432 |
| Proxy | nginx | Docker, port 8080 |

```
browser -> nginx (:8080) -+- /      -> frontend
                          +- /api/  -> backend -> database
```

## 1. Install once

| Tool | Check it works | Needed for |
|---|---|---|
| Git | `git --version` | everyone |
| Docker (Docker Desktop on Windows) | `docker compose version` | everyone |
| .NET SDK 10 | `dotnet --version` | backend |
| Node 22+ | `node --version` | frontend |

## 2. Get the code

```
git clone <repo-url>
cd crawlspace-forum
```

Create your settings file (it is never committed):

```
cp .env.example .env          # Windows: copy .env.example .env
```

Leave the default values as they are.

## 3. Run it for development

Open three terminals in the repo root.

**Terminal 1: database** (start once, it keeps running)

```
docker compose -f docker-compose.dev.yml up -d
```

**Terminal 2: backend**

```
cd backend
dotnet watch run --urls http://localhost:8000
```

Check it: open http://localhost:8000/health or http://localhost:8000/posts

**Terminal 3: frontend**

```
cd frontend
npm install
npm run dev
```

Open the address it prints (http://localhost:5173).

Working only on the frontend? You still need terminals 1 and 2 running.

## 4. Run the whole app in Docker

Use this to check everything works together before opening a PR.

```
docker compose up -d --build
```

Open http://localhost:8080. Stop with `docker compose down`.

## Useful commands

| Command | What it does |
|---|---|
| `docker compose ps` | which containers are running |
| `docker compose logs -f backend` | live logs of one service |
| `docker compose -f docker-compose.dev.yml stop` | stop the dev database (data is kept) |
| `docker compose -f docker-compose.dev.yml down -v` | delete the dev database and its data |

## Database design

PostgreSQL, **code-first**: the tables are defined by C# classes, not by hand-written SQL.

```
backend/Models/*.cs  ->  backend/Migrations/ (generated SQL)  ->  tables in Postgres
```

- `backend/Models/`: one class per table. Property = column.
- `backend/Data/AppDbContext.cs`: lists the tables (`DbSet<...>`). Use `db.Posts` in code to read and write.
- `backend/Migrations/`: generated history of schema changes. Never edit by hand, always commit.
- On startup the backend applies any new migrations by itself.

### Current schema

**Posts**

| Column | Type | Notes |
|---|---|---|
| `Id` | integer | primary key, auto-increments |
| `Content` | text | required |
| `CreatedAt` | date/time | set by the server, always UTC |

### Conventions

- Table names plural (`Posts`), model classes singular (`Post`).
- Primary key is always `Id`, an auto-increment integer.
- Store all times in UTC.
- The server sets `Id` and `CreatedAt`. Clients never send them.

### Look at the data

```
docker compose -f docker-compose.dev.yml exec db psql -U forum -d forum -c 'select * from "Posts";'
```

## Changing the database

Edit the model classes in `backend/Models`, then from `backend/`:

```
dotnet tool install --global dotnet-ef     # once
dotnet ef migrations add WhatYouChanged
```

Commit the new files in `backend/Migrations/`. The database updates itself when the backend starts.

## Problems

| Problem | Fix |
|---|---|
| `port 5432 is already allocated` | another Postgres is running. Stop it, or change the port to `5433` |
| `password authentication failed` | you changed the password in `.env` after the first start. Run `docker compose -f docker-compose.dev.yml down -v` and start again |
| `Connection refused` on the backend | the dev database isn't running (terminal 1) |
| 502 Bad Gateway on :8080 | `docker compose logs backend` |
| Code change not visible in Docker | add `--build` |

More: see [CONTRIBUTING.md](CONTRIBUTING.md) for how we work with Git.