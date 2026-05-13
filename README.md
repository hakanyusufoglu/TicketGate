# TicketGate

TicketGate is a .NET 10 modular monolith for a ticket sales platform.

## Local Infrastructure

Docker assets live under `infrastructure/`.

PostgreSQL is exposed on host port `55432` to avoid collisions with a locally installed PostgreSQL service.

Run local services:

```powershell
docker compose -f infrastructure/docker/docker-compose.yml up -d
```

## Identity Migrations

Create the initial Identity migration:

```powershell
dotnet ef migrations add Init_Identity --project src/Modules/TicketGate.Identity --startup-project src/TicketGate.API
```

## Baslangic

Gelistirme ortamini baslat:

```powershell
docker compose -f infrastructure/docker/docker-compose.yml up -d postgres redis
```

Identity migration olustur:

```powershell
dotnet ef migrations add Init_Identity `
  --project src/Modules/TicketGate.Identity `
  --startup-project src/TicketGate.API `
  --output-dir Infrastructure/Persistence/Migrations
```

Identity migration uygula:

```powershell
dotnet ef database update `
  --project src/Modules/TicketGate.Identity `
  --startup-project src/TicketGate.API
```

API baslat:

```powershell
dotnet run --project src/TicketGate.API
```

Swagger:

```text
http://localhost:5000/swagger
```
