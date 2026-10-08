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

Docker Compose runs:

- API
- PostgreSQL
- Redis

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