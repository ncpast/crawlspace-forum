# Crawlspace Forum

Imageboard-style forum. University team project.

| Part | Tech | Where it runs |
|---|---|---|
| Frontend | Astro (server-rendered, React components) | Node, port 4321 |
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
| Node 22.23.4+ | `node --version` | frontend |

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

Check it: open http://localhost:8000/health or http://localhost:8000/boards

Keep the `--urls http://localhost:8000` part. The frontend expects the API on port 8000; without it the backend starts on a different port and the pages can't load data.

**Terminal 3: frontend**

```
cd frontend
npm install
npm run dev
```

Open http://localhost:4321. Boards are at `/tech` and `/random`; `/status` shows whether the frontend can reach the backend.

In dev, the frontend forwards `/api/...` to the backend on port 8000, same as nginx does in Docker.

Working only on the frontend? You still need terminals 1 and 2 running.

## 4. Run the whole app in Docker

Use this to check everything works together before opening a PR.

```
docker compose up -d --build
```

Open http://localhost:8080. The API is under http://localhost:8080/api/ (e.g. `/api/health`). Stop with `docker compose down`.

Docker uses its own database, separate from the dev one, so data from one won't show up in the other. Only port 8080 is opened on your machine, so this can run alongside the dev setup.

## Useful commands

| Command | What it does |
|---|---|
| `docker compose ps` | which containers are running |
| `docker compose logs -f backend` | live logs of one service |
| `docker compose -f docker-compose.dev.yml stop` | stop the dev database (data is kept) |
| `docker compose -f docker-compose.dev.yml down -v` | delete the dev database and its data |
| `docker compose down -v` | delete the Docker (port 8080) database and its data |

## Database design

PostgreSQL, **code-first**: the tables are defined by C# classes, not by hand-written SQL.

```
backend/Models/*.cs  ->  backend/Migrations/ (generated SQL)  ->  tables in Postgres
```

- `backend/Models/`: one class per table. Property = column.
- `backend/Data/AppDbContext.cs`: lists the tables (`DbSet<...>`). Use `db.Posts` in code to read and write.
- `backend/Migrations/`: generated history of schema changes. Never edit by hand, always commit.
- On startup the backend applies any new migrations by itself.

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
| Pages load but show no data / `fetch failed` in the frontend terminal | the backend isn't running, or isn't on port 8000 |
| `port 4321` or `8000` already in use | an old frontend or backend is still running. Stop it (`npx astro dev stop` in `frontend/` for the frontend) |
| 500 or 502 on :8080 | the backend container probably crashed. Check `docker compose ps` and `docker compose logs backend` |
| Backend crashes on start with `violates foreign key constraint` | your database was created by an old schema and has data the new migrations can't fit. Delete it and start fresh: `docker compose down -v` (Docker) or `docker compose -f docker-compose.dev.yml down -v` (dev) |
| Code change not visible in Docker | add `--build` |

More: see [CONTRIBUTING.md](CONTRIBUTING.md) for how we work with Git.