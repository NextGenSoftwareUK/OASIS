# Database Migrations Guide

This document provides instructions on how to manage database migrations and updates for the **Quantum Street Exchange** project.

## Prerequisites

Ensure you have the following installed:

- **.NET SDK** (latest version recommended)
- **Entity Framework Core CLI** (install if needed using `dotnet tool install --global dotnet-ef`)
- **PostgreSQL** (or the configured database for the project)

## Navigate to API Directory

Before running any migration commands, navigate to the API project directory:

```sh
cd oasis-bridge/backend/src/api
```

## Creating a New Migration

To create a new migration, use the following command. Replace `"MigrationName"` with a descriptive name for the
migration:

```sh
dotnet ef migrations add "MigrationName" -p ./Infrastructure/ -s ./API/ --output-dir DataAccess/Migrations
```

## Applying Migrations to the Database

To apply all pending migrations and update the database schema, use the following command:

```sh
dotnet ef database update -p ./Infrastructure/ -s ./API/
```
## Command for delete Database

Use the following command:

```sh
dotnet ef database drop -p ./Infrastructure/ -s ./API/


