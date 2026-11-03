# Decision Log

## 2026-11-03 / Migrate Backend to ASP.NET Core Web API + PostgreSQL
- **Files Changed**:
  - `new-backend-server/Bitlink.sln`
  - `new-backend-server/Bitlink.Api/`
  - `new-backend-server/Bitlink.Tests/`
  - `new-backend-server/docker-compose.yml`
  - `new-backend-server/.env`
  - `new-backend-server/docs/CODE_EXPLANATION.md`
  - `new-backend-server/docs/README.md`
  - `frontend-server/src/contexts/SocketContext.tsx`
  - `frontend-server/package.json`
  - `docs/PROJECT_MAP.md`
  - `docs/CONTEXT.md`
  - `docs/DECISIONS.md`
- **Why**:
  - Migrated Bitlink backend from Node.js/Express, MongoDB, and Redis to ASP.NET Core Web API with .NET 10 LTS and PostgreSQL running in Docker.
  - Replaced MongoDB collections (`sessions`, `files`, `fileUploads`) with normalized relational PostgreSQL tables managed via Entity Framework Core 10 and Npgsql.
  - Replaced Redis distributed locking and Lua scripts with PostgreSQL transaction-scoped advisory locks (`pg_advisory_xact_lock(hashtext('refresh:' || sessionId))`), guaranteeing atomic concurrency control during refresh-token rotation across distributed backend instances.
  - Integrated MinIO S3 object storage using AWSSDK.S3 for presigned upload and download URLs and binary management.
  - Implemented ASP.NET Core SignalR (`FileHub`) replacing Socket.IO, and integrated an adapter in the frontend's `SocketContext.tsx` to maintain 100% compatibility with existing React UI and file management flows.
  - Added comprehensive xUnit test suite with 18 unit tests covering authentication, session validation, advisory locking concurrency, and file operations.
  - Authored full mandatory documentation in `new-backend-server/docs/CODE_EXPLANATION.md` and `new-backend-server/README.md`.


## 2026-10-22 / Add MIT License File & Update README
- **Files Changed**:
  - `LICENSE`
  - `README.md`
  - `docs/PROJECT_MAP.md`
  - `docs/DECISIONS.md`
- **Why**:
  - Added official `LICENSE` file containing standard MIT License terms (Copyright 2026 Bitlink Contributors).
  - Updated `README.md` License section to reflect the MIT License.
  - Registered `LICENSE` in `docs/PROJECT_MAP.md` under root configuration.


## 2026-10-22 / Comprehensive Root README Documentation with Truth OAuth Integration Guide
- **Files Changed**:
  - `README.md`
  - `docs/PROJECT_MAP.md`
  - `docs/DECISIONS.md`
- **Why**:
  - Created a comprehensive, production-ready `README.md` in the base directory of the Bitlink project.
  - Documented the dependency on the external Truth OAuth 2.0 Authorization Server (`https://github.com/rahuls10x/Truth`), including registration steps, PKCE token exchange, environment variable mappings, and client credential setup.
  - Added architectural system diagrams and Mermaid sequence flows illustrating the PKCE authorization flow, token refresh rotation, and real-time Socket.io file operations.
  - Documented full configuration options for backend and frontend environments, MinIO S3 object storage setup, REST endpoints, WebSocket events, and development/testing instructions.


## 2026-10-22 / Fix DropdownMenuLabel MenuGroupContext Error in DashboardLayout
- **Files Changed**:
  - `frontend-server/src/layout/DashboardLayout.tsx`
  - `docs/DECISIONS.md`
- **Why**:
  - Wrapped `DropdownMenuLabel` inside `DropdownMenuGroup` in both the sidebar footer user profile dropdown and the top navbar user dropdown.
  - Base UI's `Menu.GroupLabel` requires a parent `Menu.Group` context (`MenuGroupContext`) to manage accessible labelling, resolving the runtime error `Base UI: MenuGroupContext is missing. Menu group parts must be used within <Menu.Group> or <Menu.RadioGroup>.`.


## 2026-10-21 / Frontend Logout, User Profile Display & Pre-Logout WebSocket Disconnect
- **Files Changed**:
  - `frontend-server/src/config/routes.ts`
  - `frontend-server/src/contexts/AuthContext.tsx`
  - `frontend-server/src/contexts/SocketContext.tsx`
  - `frontend-server/src/layout/DashboardLayout.tsx`
  - `frontend-server/src/feature/dashboard/DashboardPage.tsx`
  - `backend-server/tests/AuthController.test.ts`
  - `docs/CONTEXT.md`
  - `docs/PROJECT_MAP.md`
  - `docs/DECISIONS.md`
- **Why**:
  - Registered `/api/auth/logout` route and integrated backend session invalidation via `POST /api/auth/logout` with `credentials: "include"`.
  - Added `registerBeforeLogout` hook in `AuthContext` to coordinate teardown actions before calling the logout endpoint, guaranteeing that the WebSocket connection in `SocketContext` is cleanly disconnected prior to session cookie destruction.
  - Bound real user profile data (`name`, `email`, `avatar`, and fallback initials) received from `/api/auth/token` and `/api/auth/me` into `DashboardLayout` (sidebar footer profile and top navbar dropdown) and `DashboardPage` (welcome banner and User Account card).
  - Updated `backend-server/tests/AuthController.test.ts` to match controller response schema (`{ sessionId, name, email }`) and added unit test coverage for `me` and `logout` controller handlers.


## 2026-10-21 / Consolidate File Upload Handling in FileContext
- **Files Changed**:
  - `frontend-server/src/components/UploadModal.tsx`
  - `docs/PROJECT_MAP.md`
  - `docs/DECISIONS.md`
- **Why**:
  - Removed duplicate WebSocket emissions (`requestingToUpload`, `uploadSuccess`, `uploadFailed`) and manual `XMLHttpRequest` instantiation from `UploadModal.tsx`.
  - Replaced it with the centralized `uploadFile` method from `FileContext` (`useFiles()`), passing file binaries and tracking upload progress via callback.
  - Ensured single source of truth for upload authorization, S3 transfers, error reporting, and sender local file list updates upon `uploadSuccess` acknowledgment.


## 2026-10-21 / Real-Time File Concurrency & Sender Ack Local State Updates
- **Files Changed**:
  - `frontend-server/src/types/index.ts`
  - `frontend-server/src/contexts/FileContext.tsx`
  - `frontend-server/src/components/UploadModal.tsx`
  - `docs/CONTEXT.md`
  - `docs/DECISIONS.md`
- **Why**:
  - Handled real-time concurrent file updates across user sessions by listening to the WebSocket `fileUpdated` event (`fileAdded`, `fileRenamed`, `fileDeleted`) emitted by the backend to all instances except the sender.
  - Implemented immediate local state mutations for the initiating sender upon receiving positive acknowledgments (`ack`):
    - `deleteFile`: Removes the file locally from the `files` array on successful delete acknowledgment.
    - `renameFile`: Renames the file locally with updated `fileName` and `modifiedAt` timestamp on successful rename acknowledgment.
    - `uploadSuccess`: Receives the newly created `ServerFile` in the acknowledgment response (`ackRes.data`) and adds it directly to the sender's local `files` array in both `uploadFile` and `UploadModal`.


## 2026-10-20 / Centralize File Operations in FileContext & Dedicated Files Page
- **Files Changed**:
  - `frontend-server/src/contexts/FileContext.tsx`
  - `frontend-server/src/feature/files/FilesPage.tsx`
  - `frontend-server/src/feature/dashboard/DashboardPage.tsx`
  - `frontend-server/src/layout/DashboardLayout.tsx`
  - `frontend-server/src/components/UploadModal.tsx`
  - `frontend-server/src/config/routes.ts`
  - `frontend-server/src/types/index.ts`
  - `frontend-server/src/App.tsx`
  - `docs/CONTEXT.md`
  - `docs/PROJECT_MAP.md`
  - `docs/DECISIONS.md`
- **Why**:
  - Centralized all file state (`files`, `isLoading`, `error`, `processingFileId`, `isUploadModalOpen`) and WebSocket event methods (`fetchFiles`, `downloadFile`, `previewFile`, `renameFile`, `deleteFile`, `uploadFile`) into a unified `FileContext` (`useFiles()`).
  - Created a dedicated feature page `FilesPage.tsx` mounted at `/files` for viewing and managing files with search, previews, downloads, renaming, and deletion.
  - Reverted `DashboardPage.tsx` at `/dashboard` to a clean overview page displaying summary statistics, user details, and live microservice API testing.
  - Completely eliminated the file share action and removed the manual refresh system (buttons, state, and outlet context).
  - Ensured that mutating operations (uploading, renaming, deleting) do not modify or re-fetch the local `files` array, per explicit project instructions.


## 2026-10-19 / Connect Frontend Dashboard with Backend WebSocket File Events
- **Files Changed**:
  - `frontend-server/src/types/index.ts`
  - `frontend-server/src/components/RenameModal.tsx`
  - `frontend-server/src/components/DeleteConfirmModal.tsx`
  - `frontend-server/src/feature/dashboard/DashboardPage.tsx`
  - `frontend-server/src/layout/DashboardLayout.tsx`
  - `docs/CONTEXT.md`
  - `docs/PROJECT_MAP.md`
  - `docs/DECISIONS.md`
- **Why**: Replaced placeholder static file list in DashboardPage with live WebSocket communication. Integrated `viewFiles` event to fetch and display uploaded files with responsive skeletons and empty state, `requestingToDownload` to generate secure presigned URLs for native browser downloads and new-tab previews, `renameFile` with a dedicated validation modal for renaming files, and `deleteFile` with a confirmation dialog modal for permanent deletion. Configured DashboardLayout outlet context to automatically re-fetch files whenever a file upload completes in UploadModal.


## 2026-10-19 / Frontend Uppy File Upload System & WebSocket Integration
- **Files Changed**: 
  - `frontend-server/src/components/UploadModal.tsx`
  - `frontend-server/src/layout/DashboardLayout.tsx`
  - `docs/PROJECT_MAP.md`
  - `docs/DECISIONS.md`
- **Why**: Implemented the frontend file upload system using Uppy core (`@uppy/core`) integrated with Socket.io real-time events (`requestingToUpload`, `uploadSuccess`, `uploadFailed`) and direct PUT requests to MinIO S3 presigned URLs. Wired the upload modal into the Google Drive-style DashboardLayout sidebar and top bar upload buttons.

## 2026-10-19 / Backend Audit & Verification of Upload Controller and WebSockets
- **Files Changed**: `docs/DECISIONS.md`
- **Why**: Completed a comprehensive review and audit of the backend upload controller (`UploadController.ts`), upload service (`UploadService.ts`), repositories, validation schemas, and WebSocket authentication middleware (`TokenAuthMiddleware.ts`). Verified that all components adhere strictly to architectural patterns, security standards, and JSDoc flow guidelines, and successfully executed the test suite with all unit tests passing.


## 2026-10-17 / WebSocket "requestingToUpload" Event Listener & UploadService
- **Files Changed**: backend-server/src/services/CryptoService.ts, backend-server/src/services/UploadService.ts, backend-server/src/services/SocketService.ts, docs/PROJECT_MAP.md, docs/DECISIONS.md
- **Why**: Implemented `UploadService` and added the `requestingToUpload` WebSocket event listener in `SocketService` to securely generate upload tokens and return `{ tokenId, signedUploadLink }` responses in adherence with architectural style guide and JSDoc flow standards.

## 2026-10-18 / MinIO S3 Client Initialization in server.ts
- **Files Changed**: `backend-server/server.ts`, `docs/DECISIONS.md`
- **Why**: Initialized the AWS SDK v3 `S3Client` for MinIO in `backend-server/server.ts` alongside MongoDB and Redis connections, passing the client instance and bucket name into `BucketRepository`.

## 2026-10-18 / UploadRepository & UploadService Integration
- **Files Changed**: `backend-server/src/services/UploadService.ts`, `docs/PROJECT_MAP.md`, `docs/DECISIONS.md`
- **Why**: Integrated `UploadRepository` into `UploadService` to persist file upload records in MongoDB and cache upload tokens in Redis with TTL expiration when generating signed upload URLs.


## Task: Google Drive-Style Cloud Storage UI Frontend
- **Date / Task**: October 17, 2026 / Google Drive UI Redesign
- **Files Changed**: 
  - `d:\Code\bitlink\frontend-server\src\layout\DashboardLayout.tsx`
  - `d:\Code\bitlink\frontend-server\src\feature\dashboard\DashboardPage.tsx`
  - `docs/PROJECT_MAP.md`
  - `docs/CONTEXT.md`
  - `docs/DECISIONS.md`
- **Why**: Redesigned the frontend layout and dashboard to mirror a clean, modern Google Drive cloud storage interface. Added a left sidebar with a prominent Upload button and "My Files" navigation, a top navigation bar with profile avatar and breadcrumbs, and a responsive file grid with file type icons, sizes, modification dates, and three-dot action menus with placeholder action handlers.

## 2026-10-13 / Session A Bucket Repository Implementation
- **Files Changed**: backend-server/.env, backend-server/example.env, backend-server/package.json, backend-server/src/repositories/BucketRepository.ts, docs/PROJECT_MAP.md, docs/CONTEXT.md, docs/DECISIONS.md
- **Why**: Implemented BucketRepository wrapping AWS SDK v3 S3Client for MinIO integration, providing methods for upload and download signed URLs, file deletion, existence checks, and metadata retrieval following architectural style guide and JSDoc flow conventions.


## 2026-10-12 / SessionService & SocketService Architecture Refactor
- **Files Changed**: backend-server/server.ts, backend-server/src/services/SessionService.ts, backend-server/src/services/SocketService.ts, backend-server/src/middlewares/TokenAuthMiddleware.ts, frontend-server/src/contexts/SocketContext.tsx, frontend-server/src/App.tsx, docs/PROJECT_MAP.md, docs/CONTEXT.md, docs/DECISIONS.md
- **Why**: Created `SessionService` to encapsulate session validation and token refreshing logic shared by `TokenAuthMiddleware` and the newly implemented `SocketService`. Wrapped Express with `http.createServer(app)` to share the TCP port and listener with Socket.io.


## 2026-10-12 / WebSocket Authentication & Connection Lifecycle
- **Files Changed**: backend-server/server.ts, frontend-server/src/contexts/SocketContext.tsx, frontend-server/src/App.tsx, docs/PROJECT_MAP.md, docs/CONTEXT.md, docs/DECISIONS.md
- **Why**: Implemented real-time WebSocket connection using Socket.io between backend and frontend right after initial authentication. Handled cookie-based session verification during handshake, automated connection upon successful authentication, and clean disconnection upon logout.


﻿# Decision Log

## 2026-10-11 / Documentation & Architecture Synchronization
- **Files Changed**: docs/CONTEXT.md, docs/DECISIONS.md`n- **Why**: Initialized core system context (docs/CONTEXT.md) and decision log (docs/DECISIONS.md) following the Cline rules and junior developer workflow guidelines, accurately reflecting the microservice architecture, OAuth PKCE flow, MongoDB, Redis, Express backend, and React 19 frontend.

- **Date / Task**: 10/19/2026 / Implement Frontend WebSocket Client Integration for File Events
- **Files Changed**: frontend-server/src/feature/dashboard/DashboardPage.tsx
- **Why**: Integrated useSocket() and socket events (viewFiles, requestingToDownload, renameFile, deleteFile) into the dashboard page for dynamic file loading, secure browser downloads, renaming, and deletion.
