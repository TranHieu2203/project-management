# project-management (Story 1.0)

## Prereqs
- Docker Desktop (running)
- .NET SDK 10
- Node.js 20+

## Run (Docker: DB + API + Web)

```bash
docker compose up -d --build
```

- Web: `http://localhost:5173`
- API health: `http://localhost:8080/health`

## Run (local dev)

### Database

```bash
docker compose up -d db
```

### API

```bash
dotnet run --project src/Host/ProjectManagement.Host/ProjectManagement.Host.csproj
```

### Web

```bash
cd frontend/project-management-web
npm start
```

## Seeded dev user (DB)

- email: `pm1@local.test`
- displayName: `PM One`
- passwordHash: `dev-only` (placeholder for Story 1.0)

## Tests

### Backend

```bash
dotnet test
```

### Frontend unit tests

```bash
cd frontend/project-management-web
npm test
```

### E2E (Playwright)

```bash
cd frontend/project-management-web
npm run e2e
```

## Tear down

```bash
docker compose down -v
```

