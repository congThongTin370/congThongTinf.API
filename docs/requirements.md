# Requirements

## Project Overview

Internal news portal for the 370 Air Division (Sư đoàn Không Quân 370). A simple MVP web application for managing and viewing internal news and documents.

## User Roles

### Admin
- View dashboard and statistics
- Create/manage user accounts
- Assign roles to users
- Upload and manage internal division documents

### Journalist
- Create news articles
- Edit their own articles
- Publish articles
- Manage their own articles
- View published news

## News Article Model

A news article contains:

- **Title** - Article headline
- **Summary** - Brief description
- **Content** - Full article body
- **Cover Image** - Article thumbnail/featured image
- **Category** - News category/classification
- **Author** - Journalist who created the article
- **Status** - Current state of the article
- **PublishedAt** - Publication timestamp
- **CreatedAt** - Creation timestamp
- **UpdatedAt** - Last modification timestamp

### Article Status Values
- **Draft** - Not yet submitted for review
- **Pending** - Awaiting admin approval
- **Published** - Available to view
- **Archived** - Historical articles

## UI/UX Requirements

- Simple, clean design similar to standard newspaper layouts
- Can be simplified further for MVP
- Responsive design for internal use
- Server-side rendering preferred (ASP.NET MVC + Razor)

## Technology Stack

- Backend: ASP.NET Core 10 (C#)
- Database: PostgreSQL
- Caching: Redis
- Containerization: Docker
- Architecture: Domain-Driven Design with layered architecture
