# Project Instructions

## Project

Internal news portal for the 370 Air Division.

The system has two major user roles:

- Admin
- Journalist

Admin:
- View dashboard/statistics
- Manage user accounts and roles
- Manage internal documents

Journalist:
- Create news
- Edit news
- Publish news
- Manage their own articles

## Technology

Backend:
- ASP.NET Core 10
- C#
- PostgreSQL
- Redis
- Docker
- Domain-Driven Design

Backend projects:
- congThongTin.API
- congThongTin.Service
- congThongTin.Domain
- congThongTin.Infrastructure
- congThongTin.Repository

## General Rules

- Preserve the existing architecture.
- Do not introduce a new architectural pattern unless explicitly requested.
- Keep business logic out of Controllers.
- Do not access the database directly from Controllers.
- Do not modify unrelated files.
- Before making large changes, inspect the existing implementation and explain the proposed approach.
- Prefer existing project patterns over inventing new ones.
- Keep implementations simple and appropriate for an MVP.
- Do not add dependencies unless necessary.
- Do not run the EF Core CLI (`dotnet ef`, `Add-Migration`, `Update-Database`, `Remove-Migration`). The API applies pending migrations itself on startup through `Database.Migrate()`.