# Innovayse Docs

A real-time collaborative document editor — Google-Docs-style rich text editing with
multi-user presence, comments, folder sharing, PDF export, and document tabs.

## Architecture

The project is split into three services:

| Service | Path | Stack |
|---|---|---|
| API | `backend/` | .NET 9, ASP.NET Core, EF Core (PostgreSQL) |
| Collaboration server | `collab/` | Node.js, TypeScript, Hocuspocus, Yjs |
| Frontend | `frontend/` | Nuxt 4, Vue 3, TipTap |

The frontend talks to the API for document/folder CRUD, auth, comments, and sharing, and
connects to the collab server via WebSocket for real-time co-editing (Yjs CRDT sync).

## Prerequisites

- .NET 9 SDK
- Node.js 20+
- PostgreSQL

## Setup

### Backend (API)

```bash
cd backend
cp src/Innovayse.Docs.API/appsettings.Example.json src/Innovayse.Docs.API/appsettings.Development.json
# edit the connection string to point at your PostgreSQL instance
dotnet ef database update --project src/Innovayse.Docs.Infrastructure --startup-project src/Innovayse.Docs.API
dotnet run --project src/Innovayse.Docs.API
```

The API listens on `http://localhost:5259` by default.

### Collaboration server

```bash
cd collab
cp .env.example .env
npm install
npm run dev
```

`DOCS_API_BASE_URL` must point at the running API (default `http://localhost:5259`) — the
collab server validates document access against it before allowing a Yjs sync session.

### Frontend

```bash
cd frontend
cp .env.example .env
npm install
npm run dev
```

## Testing

```bash
# backend
cd backend && dotnet test

# collab
cd collab && npm test

# frontend type check
cd frontend && npx nuxt prepare && npx vue-tsc --noEmit -p .nuxt/tsconfig.app.json
```

## License

[MIT](LICENSE)
