- `frontend-server/src/components/UploadModal.tsx`: File upload modal component managing upload queue state and delegating S3 uploads, progress reporting, and WebSocket acknowledgment events to FileContext.
- `frontend-server/src/components/RenameModal.tsx`: Modal dialog for validating and renaming user files via WebSocket event.
- `frontend-server/src/components/DeleteConfirmModal.tsx`: Confirmation dialog modal for permanently deleting files via WebSocket event.
- `frontend-server/src/feature/dashboard/DashboardPage.tsx`: Dynamic file manager dashboard connecting to backend WebSocket events for live file listing, downloads, previews, renaming, and deletion.

# Project Map

This document maps all files and modules in the microservice template with single-bullet descriptions of their purpose and responsibilities.

## Root Configuration
- `README.md`: Comprehensive project documentation covering architecture, Truth OAuth 2.0 PKCE integration, MinIO S3 streaming, real-time WebSocket events, and local/Docker deployment.
- `LICENSE`: MIT License terms and conditions for Bitlink.
- `docker-compose.yml`: Container orchestration definition for spinning up backend and frontend microservices together.
- `.clinerules`: Architectural constraints, developer role guidelines, and documentation policies.

## Documentation (`docs/`)
- `docs/CONTEXT.md`: System architecture, environment boundaries, OAuth 2.0 PKCE data flow, port allocations, and response models.
- `docs/PROJECT_MAP.md`: Index mapping each file in the workspace to its single-bullet responsibility.
- `docs/STYLEGUIDE.md`: Coding conventions, section header structure, JSDoc flow checklist rules, and error handling patterns.
- `docs/DECISIONS.md`: Chronological log of architecture, refactoring, and implementation decisions.
- `docs/Authentication Wiki.md`: Reference documentation detailing the Truth OAuth 2.0 PKCE authorization server protocol.

## New ASP.NET Core Backend (`new-backend-server/`)
- `new-backend-server/Bitlink.sln`: Visual Studio solution grouping the Web API and xUnit test projects.
- `new-backend-server/.env`: Backend environment variables derived from root configuration for local and Docker execution.
- `new-backend-server/docker-compose.yml`: Multi-container orchestration provisioning PostgreSQL 17, MinIO S3, and the ASP.NET Core API.
- `new-backend-server/README.md`: Setup, Docker, environment configuration, migrations, and execution guide for the new backend.
- `new-backend-server/docs/CODE_EXPLANATION.md`: Mandatory code reference documenting every source file, class, method, parameter, and architectural flow.
- `new-backend-server/Bitlink.Api/Program.cs`: ASP.NET Core Web API startup configuring DI, Npgsql DbContext, CORS, SignalR, and middleware pipeline.
- `new-backend-server/Bitlink.Api/Dockerfile`: Multi-stage Dockerfile targeting .NET 10 ASP.NET runtime.
- `new-backend-server/Bitlink.Api/Data/BitlinkDbContext.cs`: EF Core DbContext managing PostgreSQL tables, indexes, and transaction advisory locks.
- `new-backend-server/Bitlink.Api/Data/Entities/SessionEntity.cs`: Entity mapping for PostgreSQL `sessions` table.
- `new-backend-server/Bitlink.Api/Data/Entities/FileEntity.cs`: Entity mapping for PostgreSQL `files` table.
- `new-backend-server/Bitlink.Api/Data/Entities/FileUploadEntity.cs`: Entity mapping for PostgreSQL `file_uploads` table.
- `new-backend-server/Bitlink.Api/Repositories/SessionRepository.cs`: Data access layer for session persistence and advisory locking in PostgreSQL.
- `new-backend-server/Bitlink.Api/Repositories/FileRepository.cs`: Data access layer for file metadata CRUD in PostgreSQL.
- `new-backend-server/Bitlink.Api/Repositories/UploadRepository.cs`: Data access layer for file upload tokens in PostgreSQL.
- `new-backend-server/Bitlink.Api/Repositories/BucketRepository.cs`: Data access layer interfacing with MinIO S3 via AWSSDK.S3.
- `new-backend-server/Bitlink.Api/Services/AuthService.cs`: Business logic for Truth OAuth PKCE token exchange, user resource retrieval, and advisory-locked refresh rotation.
- `new-backend-server/Bitlink.Api/Services/SessionService.cs`: Session validation service managing token TTLs and automatic rotation triggers.
- `new-backend-server/Bitlink.Api/Services/FileService.cs`: Business logic for querying, downloading, renaming, and deleting files.
- `new-backend-server/Bitlink.Api/Services/UploadService.cs`: Business logic coordinating upload authorization, presigned URLs, and finalization.
- `new-backend-server/Bitlink.Api/Services/CryptoService.cs`: Cryptographic helper generating prefixed unique IDs (`session_`, `file_`, `upload_`, `lock_`).
- `new-backend-server/Bitlink.Api/Controllers/AuthController.cs`: REST controller providing `/api/auth/token`, `/api/auth/me`, `/api/auth/logout`, and `/health`.
- `new-backend-server/Bitlink.Api/Hubs/FileHub.cs`: SignalR real-time hub managing file operations and multi-tab synchronization.
- `new-backend-server/Bitlink.Api/Middleware/SessionAuthAttribute.cs`: Action filter enforcing cookie-based session authentication on protected routes.
- `new-backend-server/Bitlink.Api/Middleware/ExceptionHandlingMiddleware.cs`: Global middleware converting uncaught errors and `BitlinkException` into standard JSON responses.
- `new-backend-server/Bitlink.Tests/AuthControllerTests.cs`: xUnit test suite for authentication endpoints, validation, and cookie handling.
- `new-backend-server/Bitlink.Tests/SessionServiceTests.cs`: xUnit test suite for session validation, TTL revocation, and access token rotation.
- `new-backend-server/Bitlink.Tests/FileServiceTests.cs`: xUnit test suite covering file queries, download permissions, renaming, and deletion.
- `new-backend-server/Bitlink.Tests/RefreshTokenConcurrencyTests.cs`: xUnit test suite verifying transaction-scoped advisory locks during concurrent refresh requests.


## Backend Microservice (`backend-server/`)
- `backend-server/server.ts`: Express application bootstrap configuring middleware, registering API routes, and managing graceful shutdown.
- `backend-server/.env`: Local environment variables for port, origin URL, Truth server endpoints, and client credentials.
- `backend-server/example.env`: Sample environment file template for backend deployment and onboarding.
- `backend-server/eslint.config.js`: ESLint flat configuration enforcing TypeScript standards and node global types.
- `backend-server/package.json`: Backend Node.js dependencies, development tooling (TypeScript, TSX, Vitest), and build scripts.
- `backend-server/tsconfig.json`: TypeScript compiler configuration targeting ESNext and NodeNext module resolution with dist output.
- `backend-server/src/controllers/AuthController.ts`: HTTP request handler for OAuth PKCE authorization code exchange and session cookie issuance.
- `backend-server/src/services/AuthService.ts`: Business logic layer executing confidential token exchange (`POST /tokenize`) and refresh rotation (`POST /refresh`) against Truth Auth Server.
- `backend-server/src/services/CryptoService.ts`: Cryptographic utility generating session identifiers and distributed lock keys.
- `backend-server/src/repositories/SessionRepository.ts`: MongoDB data access repository managing user session documents and TTL indexes.
- `backend-server/src/repositories/TokenRepository.ts`: Redis data access repository managing distributed lock acquisition, renewal, and release.
- `backend-server/src/middlewares/TokenAuthMiddleware.ts`: Express middleware checking session validity, refreshing expired tokens, and attaching session credentials to requests.
- `backend-server/src/middlewares/ValidationMiddlewares.ts`: Input validation handlers enforcing parameter formats for token exchange.
- `backend-server/src/repositories/BucketRepository.ts`: AWS S3 / MinIO data access repository managing signed URLs, file existence checks, metadata retrieval, and deletion.
- `backend-server/src/types/dbSchema.ts`: MongoDB document schemas and session collection type definitions.
- `backend-server/src/types/index.ts`: TypeScript interfaces and Express Request interface augmentations for session credentials and tokens.
- `backend-server/src/utils/ApplicationError.ts`: Custom error class, non-null assertion helper (`strictCheck`), and standardized response formatter (`errorHandling`).
- `backend-server/src/utils/releaseLock.lua.ts`: Lua script ensuring atomic Redis distributed lock release.
- `backend-server/src/services/SessionService.ts`: Centralized session validation, expiration checks, and token rotation business logic service.
- `backend-server/src/services/SocketService.ts`: Dedicated Socket.io WebSocket service handling authenticated real-time connection and disconnection lifecycle.
- `backend-server/src/services/UploadService.ts`: Business logic service coordinating upload token generation and presigned upload URL requests with BucketRepository.
- `backend-server/src/repositories/UploadRepository.ts`: MongoDB and Redis repository handling file upload document creation and token caching.

- `backend-server/node_modules/socket.io/`: WebSocket server library for handling real-time connections.
- `backend-server/tests/AuthController.test.ts`: Vitest suite verifying the token exchange endpoint, request validation, and HTTP-only cookie configuration.
- `backend-server/tests/TokenAuthMiddleware.test.ts`: Vitest suite covering session retrieval, refresh token expiration, and asynchronous access token rotation including lock scenario checks.
## Frontend Application (`frontend-server/`)
- `frontend-server/.env`: Local frontend environment configuration (client ID, redirect URI, API URL, Truth authorize URL).
- `frontend-server/example.env`: Sample environment file template for frontend development and onboarding.
- `frontend-server/nginx.conf`: Nginx web server configuration providing SPA fallback routing for production Docker containers.
- `frontend-server/eslint.config.js`: ESLint flat configuration for React 19, TypeScript, and Hooks.
- `frontend-server/package.json`: Frontend React 19, Vite, Tailwind CSS, Lucide icons, and Radix UI dependencies.
- `frontend-server/vite.config.ts`: Vite build and dev server configuration with path alias resolution.
- `frontend-server/src/App.tsx`: Application root router mounting public, auth callback, and protected dashboard routes.
- `frontend-server/src/main.tsx`: React DOM client entry point rendering the root application tree.
- `frontend-server/src/config/routes.ts`: Centralized route constants for frontend client navigation and backend API endpoints including logout.
- `frontend-server/src/lib/constants.ts`: Application-wide naming and metadata constants.
- `frontend-server/src/lib/utils.ts`: Tailwind CSS class name merging utility helper (`cn`).
- `frontend-server/src/types/index.ts`: TypeScript interfaces for authenticated users, token envelopes, and AuthContext state.
- `frontend-server/src/utils/utils.ts`: Web Crypto API PKCE helpers for code verifier generation, SHA-256 code challenge computation, and state generation.
- `frontend-server/src/hooks/useSessionStorage.tsx`: React hook for safe storage and retrieval of OAuth PKCE verifiers and state tokens.
- `frontend-server/src/hooks/use-mobile.ts`: Responsive screen viewport detection hook for sidebar auto-collapse.
- `frontend-server/src/contexts/AuthContext.tsx`: React Context provider managing PKCE authorization redirects, backend token exchanges, session restoration, pre-logout teardown hooks, and server logout.
- `frontend-server/src/contexts/SocketContext.tsx`: React Context provider managing WebSocket connection lifecycle upon user authentication and graceful disconnect prior to logout.
- `frontend-server/src/contexts/FileContext.tsx`: React Context provider centralizing file data, loading/error states, upload modal state, and file socket actions.
- `frontend-server/src/components/ProtectedRoute.tsx`: Route guard ensuring only authenticated users access protected microservice routes.
- `frontend-server/src/layout/DashboardLayout.tsx`: Shell layout with brand header, sidebar navigation, user profile avatar and initials display, pre-logout socket disconnect trigger, and `<Outlet />`.
- `frontend-server/src/feature/auth/AuthPages.tsx`: Presentation components for Login (`LoginPage`) and OAuth PKCE callback processing (`CallbackPage`).
- `frontend-server/src/feature/dashboard/DashboardPage.tsx`: Dashboard overview showcasing authentication status, user identity, file count, and live protected API testing.
- `frontend-server/src/feature/files/FilesPage.tsx`: Dedicated cloud file management page offering file grid, search, previews, downloads, renaming, and deletion.
- `frontend-server/src/assets/Logo.tsx`: SVG vector logo component for Truth OAuth.
- `frontend-server/src/components/ui/`: Reusable UI primitives (Avatar, Breadcrumb, Button, Card, DropdownMenu, Separator, Sidebar, Spinner, Tooltip, etc.).
