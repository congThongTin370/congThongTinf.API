architecture# Architecture

## Projects

- congThongTin.API
  - HTTP/API layer
  - Controllers
  - Authentication/Authorization

- congThongTin.Service
  - Application layer
  - Use cases
  - DTOs
  - Application services

- congThongTin.Domain
  - Entities
  - Value Objects
  - Domain logic
  - Domain events

- congThongTin.Infrastructure
  - EF Core
  - PostgreSQL
  - Redis
  - External services
  - File storage

- congThongTin.Repository
  - Persistence-related implementations

## Infrastructure

Docker Compose runs two stacks on the shared network `congthongtinf`:

- `congthongtinf` (`compose.yml`): API, PostgreSQL
- `congthongtinf-storage` (`compose.storage.yml`): SeaweedFS

Local development:

```bash
.\up.bat
```

## Authentication

JWT authentication.

Role-based authorization.

Roles:

- Admin
- Journalist
- Guest
## Frontend

ASP.NET Core MVC + Razor.

Server-rendered UI.

Minimal JavaScript.

## Main Modules

Admin:
- Dashboard
- User management
- Document management

Journalist:
- News management