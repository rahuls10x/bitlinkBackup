# Architecture Context

## UI / UX Architecture
- **Cloud Storage UI**: Styled after Google Drive, featuring:
  - **Left Sidebar**: Brand header with cloud icon, prominent "My Files" navigation, and an interactive "Upload" button with placeholder handling.
  - **Top Navbar**: Breadcrumbs (`Bitlink > My Files`), sidebar toggle trigger, and user profile avatar dropdown with account management and sign-out.
  - **Dashboard File Grid**: Responsive card grid displaying realistic sample files (`.pdf`, `.png`, `.docx`, `.mp4`, `.xlsx`, `.ts`), file sizes, modification dates, file-type specific icons with color coding, and a three-dot action menu (`Preview`, `Download`, `Share`, `Rename`, `Delete`) with placeholder handlers.

﻿# System Architecture & Context

## 1. Overview
Bitlink is a microservice-based application featuring a backend Express TypeScript server (ackend-server/) and a frontend React 19 Vite application (rontend-server/). The system implements OAuth 2.0 Authorization Code Grant with PKCE (Proof Key for Code Exchange) integrating with the Truth Authorization Server.

## 2. Architecture & Microservices
- **Backend Server (ackend-server/)**: Built with Node.js, Express, TypeScript, MongoDB (Mongoose), and Redis (ioredis). Handles OAuth code exchange (POST /tokenize), token refresh (POST /refresh), session management, and secure HTTP-only cookies.
- **Frontend Application (rontend-server/)**: Built with React 19, Vite, Tailwind CSS, Lucide icons, and Radix UI. Manages client-side routing, PKCE state generation, callback verification, and authenticated dashboard views.
- **New Backend Server (`new-backend-server/`)**: Built with C# 13, .NET 10 LTS, ASP.NET Core Web API, PostgreSQL 17 (Npgsql EF Core 10), and ASP.NET Core SignalR. Replaces MongoDB and Redis with normalized PostgreSQL tables and transaction-scoped advisory locks (`pg_advisory_xact_lock`) for concurrent token rotation. Integrates with MinIO S3 object storage via AWSSDK.S3.


## 3. Data Models & State Flows
- **Sessions (ackend-server/src/repositories/SessionRepository.ts)**: Stored in MongoDB with TTL indexes for automatic expiration.
- **Tokens & Distributed Locks (ackend-server/src/repositories/TokenRepository.ts)**: Managed in Redis using atomic operations and Lua scripts (eleaseLock.lua.ts).
- **OAuth PKCE Flow**:
  1. Frontend generates code verifier and SHA-256 code challenge.
- **Object Storage (backend-server/src/repositories/BucketRepository.ts)**: Interacts with MinIO S3 bucket service for presigned uploads, downloads, file existence checks, and metadata retrieval.
  2. User redirects to Truth authorization server.
  3. Authorization code is returned to frontend callback URL.
  4. Frontend sends code and verifier to backend (POST /tokenize).
  5. Backend exchanges code for access & refresh tokens with Truth Auth Server, issuing secure HTTP-only session cookies.

- **WebSocket Real-Time Updates & File Management**: Socket.io integrated between backend and frontend for real-time events, authenticated via secure HTTP-only session cookies (`authToken`) during connection handshake. Events include `viewFiles` for retrieving root files, `requestingToDownload` for generating presigned S3 download/preview URLs, `renameFile` for updating file names, and `deleteFile` for permanent removal. All file operations, socket event handlers, upload workflows, and modal states are centralized in `FileContext` (`useFiles()`). Dedicated files page is hosted at `/files` (`FilesPage.tsx`), while `/dashboard` (`DashboardPage.tsx`) serves as an overview. File mutations (upload, rename, delete) update the sender's local file state upon operation acknowledgment (`ack`), with `uploadSuccess` adding the newly created file returned in the ack payload. Other concurrent clients in the room receive real-time updates via the `fileUpdated` broadcast event (`fileAdded`, `fileRenamed`, `fileDeleted`), keeping all active sessions synchronized.
- **Frontend Authentication & Session Lifecycle**:
  - User profile data (`name`, `email`, `avatar`) is populated from `/api/auth/token` (during PKCE exchange) and `/api/auth/me` (during session verification), and dynamically rendered across `DashboardLayout` and `DashboardPage`.
  - Frontend logout initiates a pre-logout teardown via `registerBeforeLogout` in `AuthContext`, cleanly terminating the active WebSocket connection before sending `POST /api/auth/logout` to the backend.
  - The backend invalidates the session in MongoDB and clears the HTTP-only `sessionToken` cookie, followed by frontend state reset and redirect to `/login`.
## 4. Environment Boundaries & Port Allocations
- **Backend Server Port**: 4000 (configurable via PORT in .env)
- **Frontend Server Port**: 5173 (Vite dev server) / Nginx container port 80/443.
- **Truth Auth Server Base URL**: http://localhost:4000`n- **Database & Cache**: MongoDB and Redis instances orchestrated via docker-compose.yml.

## 5. Constraints & Conventions
- Adhere strictly to Layered Dependency Injection (Controllers -> Services -> Repositories).
- Use strictCheck for nullability validation and errorHandling for error responses.
- All backend relative imports must include .js extensions.
- All methods must include JSDoc blocks with an Inorder Flow checklist and 1-to-1 step/empty line formatting.
