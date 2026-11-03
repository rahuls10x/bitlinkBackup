# Bitlink ASP.NET Core + PostgreSQL Backend ☁️📁

A high-performance C# / ASP.NET Core Web API backend for the Bitlink cloud storage system, replacing the previous Node.js/Express and MongoDB/Redis architecture with ASP.NET Core, Entity Framework Core, PostgreSQL, MinIO S3 object storage, and SignalR real-time communication.

---

## 🏗️ Architecture Overview

- **Language / Framework**: C# 13 / .NET 10 (LTS) ASP.NET Core Web API
- **Database**: PostgreSQL 17 running in Docker
- **ORM**: Entity Framework Core 10 with Npgsql provider
- **Object Storage**: S3-compatible MinIO via AWSSDK.S3
- **Real-Time Communication**: ASP.NET Core SignalR
- **Authentication**: Truth OAuth 2.0 Authorization Server (PKCE Authorization Code Grant)
- **Concurrency Control**: PostgreSQL Transaction-Scoped Advisory Locks (`pg_advisory_xact_lock`) replacing Redis locks

---

## 📁 Directory Structure

```
new-backend-server/
├── Bitlink.Api/                 # ASP.NET Core Web API Project
│   ├── Controllers/             # REST API Controllers (AuthController, etc.)
│   ├── Data/                    # EF Core DbContext, Entities, and Migrations
│   ├── Hubs/                    # SignalR Real-Time Hubs (FileHub)
│   ├── Middleware/              # Token auth and validation middlewares
│   ├── Models/                  # DTOs, request/response models
│   ├── Repositories/            # Data access layers (Session, File, Upload, Bucket)
│   ├── Services/                # Business logic (Auth, Session, File, Upload, Crypto)
│   ├── Utils/                   # Error handling, strict assertions
│   ├── Program.cs               # Application entry point & DI configuration
│   ├── appsettings.json         # Base configuration
│   └── Dockerfile               # Container build configuration
├── Bitlink.Tests/               # xUnit unit and integration tests
├── docs/
│   ├── CODE_EXPLANATION.md      # Detailed source file and method reference
│   └── README.md                # This setup and execution guide
├── docker-compose.yml           # Docker orchestration (Postgres, MinIO, Backend API)
├── .env                         # Local environment variables
└── Bitlink.sln                  # .NET Solution file
```

---

## 🚀 Quick Start & Development Setup

### 1. Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/)

### 2. Infrastructure Services (PostgreSQL & MinIO)
Start PostgreSQL and MinIO using Docker Compose:
```bash
cd new-backend-server
docker compose up -d postgres minio
```

PostgreSQL will be accessible at:
- **Host**: `localhost:5432`
- **User**: `bitlink`
- **Password**: `bitlink_secure_password`
- **Database**: `bitlink`

MinIO will be accessible at:
- **API**: `http://localhost:9000`
- **Console**: `http://localhost:9001` (User: `root`, Password: `rootpass`)

### 3. Apply Entity Framework Migrations
From the `new-backend-server` directory:
```bash
dotnet ef database update --project Bitlink.Api
```

### 4. Run the Backend API Locally
```bash
cd Bitlink.Api
dotnet run
```
The API starts listening on port `4001` (or as configured in `appsettings.json` / `.env`).

### 5. Run Tests
From the `new-backend-server` directory:
```bash
dotnet test
```

---

## ⚙️ Environment Variables

Key configuration variables defined in `.env`:

| Variable | Description | Default |
|---|---|---|
| `PORT` / `BACKEND_PORT` | HTTP port for the backend server | `4001` |
| `ORIGIN_URL` | Frontend origin for CORS | `http://localhost:5174` |
| `AUTH_SERVER_URL` | Truth OAuth authorization server base URL | `http://localhost:4000` |
| `RESOURCE_SERVER_URL` | Truth OAuth resource server base URL | `http://localhost:5000` |
| `CLIENT_ID` | OAuth 2.0 client identifier | Registered client ID |
| `CLIENT_SECRET` | OAuth 2.0 client secret | Confidential client secret |
| `REDIRECT_URI` | OAuth 2.0 redirect callback URL | `http://localhost:5174/callback` |
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string | `Host=localhost;Port=5432;Database=bitlink;...` |
| `S3_ENDPOINT` | MinIO S3 API endpoint | `http://localhost:9000` |
| `S3_BUCKET_NAME` | S3 storage bucket name | `bitlink-bucket` |
| `S3_ACCESS_KEY_ID` | S3 access key | `root` |
| `S3_SECRET_ACCESS_KEY` | S3 secret key | `rootpass` |

---

## 🔒 Refresh Token Concurrency & Advisory Locks

Unlike the Express backend which used Redis distributed locks with Lua scripts:
- Bitlink ASP.NET Core uses **PostgreSQL transaction-scoped advisory locks** (`pg_advisory_xact_lock(hashtext('refresh:' || sessionId))`).
- The lock is scoped to a database transaction and automatically released upon `COMMIT` or `ROLLBACK`.
- Concurrent refresh requests for the same session block safely at the database level.
- After lock acquisition, the session is re-queried; if already rotated by a previous request, the fresh token is returned immediately without contacting the Truth server again.
