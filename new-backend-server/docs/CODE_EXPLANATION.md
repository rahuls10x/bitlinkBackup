# Bitlink Backend - Detailed Code Explanation Reference

This document provides a comprehensive technical breakdown of every source file, class, interface, enum, method, parameter, and data flow in the Bitlink ASP.NET Core backend. It is continuously maintained as components are implemented.

---

## Architecture Overview

Bitlink backend follows a layered dependency injection architecture closely mirroring the structure and patterns of the existing Express TypeScript backend:
- **Controllers / Hubs**: Handle HTTP requests and WebSocket SignalR events, perform input validation, extract session cookies, and return standard JSON or SignalR responses.
- **Middlewares**: Authenticate incoming HTTP requests and WebSocket connections using the `sessionToken` cookie, attach session context, and intercept errors.
- **Services**: Implement core business logic (OAuth PKCE token exchange, token refresh rotation with PostgreSQL advisory locking, file management, and S3 upload lifecycle).
- **Repositories**: Encapsulate data access operations against PostgreSQL (via Entity Framework Core 10 and Npgsql) and MinIO S3 object storage (via AWSSDK.S3).
- **Data Entities**: Map PostgreSQL tables (`sessions`, `files`, `file_uploads`) with constraints, indexes, and keys.
- **Utilities / Models**: Standardized error handling (`ApplicationException`, `StrictCheck`), request/response DTOs (`StandardResponse`), and ID generation (`CryptoService`).

---

## File Explanations

*(Entries will be continuously added for each implemented module in the following sections)*

### Section Index
1. [Configuration & Startup](#1-configuration--startup)
2. [Data Access & Entities](#2-data-access--entities)
### Section 1: Configuration & Startup

#### File: Program.cs
- **Purpose**: Application bootstrap entry point. Configures dependency injection, database context with PostgreSQL Npgsql provider, CORS policy for frontend integration, routing, controllers, SignalR hubs, and HTTP pipeline middleware.
- **Dependencies**: `Microsoft.AspNetCore.Builder`, `Microsoft.EntityFrameworkCore`, `Npgsql.EntityFrameworkCore.PostgreSQL`, `Bitlink.Api.Data`.
- **Flow**: Reads `ConnectionStrings:DefaultConnection`, binds EF Core `BitlinkDbContext`, registers application services, and starts listening for HTTP and WebSocket requests.

---

### Section 2: Data Access & Entities

#### File: Bitlink.Api/Data/BitlinkDbContext.cs
- **Purpose**: Central Entity Framework Core database context managing PostgreSQL connection, relational mapping, table indexes, and transaction-scoped advisory locks.
- **Class**: `BitlinkDbContext : DbContext`
- **Properties**:
  - `DbSet<SessionEntity> Sessions`: Table for persistent user OAuth sessions.
  - `DbSet<FileEntity> Files`: Table for cloud file metadata.
  - `DbSet<FileUploadEntity> FileUploads`: Table for tracking temporary upload authorizations.
- **Methods**:
  - `OnModelCreating(ModelBuilder modelBuilder)`: Configures database schema, primary keys, and specialized indexes (such as the partial index `idx_sessions_active` where `is_revoked = false`).
  - `AcquireAdvisoryXactLockAsync(string resourceKey)`: Executes `SELECT pg_advisory_xact_lock(hashtext(@key))` in the active database transaction to prevent concurrent refresh-token rotation across distributed backend instances.
- **Flow**: Injected into Repositories for atomic database operations.

#### File: Bitlink.Api/Data/Entities/SessionEntity.cs
- **Purpose**: Relational entity mapped to the `sessions` PostgreSQL table.
- **Class**: `SessionEntity`
- **Properties**:
  - `SessionId` (`string`, PK): Prefixed session token identifier (`session_` + 64 hex characters).
  - `UserId` (`string`): Authenticated user ID issued by Truth OAuth server.
  - `CreatedAt` (`DateTime` UTC): Session creation timestamp.
  - `ExpiresAt` (`DateTime` UTC): Session expiration timestamp.
  - `IsRevoked` (`bool`): Flag denoting whether the session has been invalidated upon logout.
  - `AccessToken` (`string`): Truth OAuth access token used for resource requests.
  - `RefreshToken` (`string`): Truth OAuth refresh token used for confidential rotation.
  - `TokenType` (`string`): Token type specification (`Bearer`).
  - `AccessTokenExpiry` (`DateTime` UTC): Expiry timestamp for the active access token.
  - `RefreshTokenExpiry` (`DateTime` UTC): Expiry timestamp for the refresh token (30 days).

#### File: Bitlink.Api/Data/Entities/FileEntity.cs
- **Purpose**: Relational entity mapped to the `files` PostgreSQL table.
- **Class**: `FileEntity`
- **Properties**:
  - `FileId` (`string`, PK): Prefixed file identifier (`file_` + 64 hex characters).
  - `FileName` (`string`): Human-readable file name with extension.
  - `UserId` (`string`): Owner user ID.
  - `MimeType` (`string`): Standard MIME type of the uploaded file.
  - `SizeBytes` (`long`): Total file size in bytes.
  - `ParentId` (`string?`): Optional folder identifier (null for root).
  - `CreatedAt` (`DateTime` UTC): File upload timestamp.
  - `ModifiedAt` (`DateTime` UTC): Timestamp of last rename or update.

#### File: Bitlink.Api/Data/Entities/FileUploadEntity.cs
- **Purpose**: Relational entity mapped to the `file_uploads` PostgreSQL table, replacing the MongoDB collection and Redis upload cache.
- **Class**: `FileUploadEntity`
- **Properties**:
  - `UploadId` (`string`, PK): Prefixed upload authorization token (`upload_` + 32 hex characters).
  - `UserId` (`string`): Uploader user ID.
  - `FileName` (`string`): Target file name.
  - `MimeType` (`string`): Expected MIME type.
  - `SizeBytes` (`long`): Expected file size in bytes.
  - `FileId` (`string`): Reserved S3 storage key.
  - `ParentId` (`string?`): Target folder identifier.
  - `Status` (`string`): Upload state (`pending` or `completed`).
  - `CreatedAt` (`DateTime` UTC): Creation timestamp.
  - `ExpiresAt` (`DateTime` UTC): Authorization expiry timestamp (default 5 minutes).

---
### Section 3: Repositories

#### File: Bitlink.Api/Repositories/SessionRepository.cs
- **Purpose**: PostgreSQL data access repository for user sessions using Entity Framework Core.
- **Class**: `SessionRepository : ISessionRepository`
- **Methods**:
  - `CreateSessionAsync(SessionEntity session)`: Persists a new session record into the `sessions` table.
  - `FindActiveSessionByIdAsync(string sessionId)`: Retrieves an unrevoked session (`IsRevoked = false`) by session ID.
  - `RevokeSessionByIdAsync(string sessionId)`: Sets `IsRevoked = true` for the given session ID.
  - `UpdateSessionByIdAsync(string sessionId, string accessToken, string refreshToken, string tokenType, DateTime accessTokenExpiry, DateTime refreshTokenExpiry)`: Updates the tokens and validity timestamps.
  - `BeginTransactionAsync()`: Starts a database transaction for atomic multi-step operations.
  - `AcquireAdvisoryXactLockAsync(string resourceKey)`: Executes PostgreSQL transaction-scoped advisory lock via `pg_advisory_xact_lock(hashtext(@key))`.
- **Dependencies**: `BitlinkDbContext`.
- **Flow**: Injected into `AuthService` and `SessionService`.

#### File: Bitlink.Api/Repositories/FileRepository.cs
- **Purpose**: PostgreSQL data access repository for file metadata records.
- **Class**: `FileRepository : IFileRepository`
- **Methods**:
  - `CreateNewFileAsync(FileEntity file)`: Persists file metadata after upload confirmation.
  - `ViewFilesAsync(string userId, string? parentId, int limit, int offset)`: Queries files for a user ordered by `created_at` descending with pagination.
  - `FetchFileDataAsync(string fileId)`: Retrieves metadata for a specific file.
  - `RenameFileAsync(string fileId, string newFileName, DateTime modifiedAt)`: Updates file name and modification timestamp.
  - `DeleteFileAsync(string fileId)`: Removes file metadata from PostgreSQL.
- **Dependencies**: `BitlinkDbContext`.
- **Flow**: Injected into `FileService` and `UploadService`.

#### File: Bitlink.Api/Repositories/UploadRepository.cs
- **Purpose**: PostgreSQL repository managing pending file upload tokens. Replaces the MongoDB collection and Redis cache.
- **Class**: `UploadRepository : IUploadRepository`
- **Methods**:
  - `CreateFileUploadTokenAsync(FileUploadEntity data)`: Inserts a pending upload record with an expiration timestamp.
  - `RetrieveAndDeleteFileUploadTokenAsync(string uploadId)`: Atomically finds and deletes the upload token upon finalization or cancellation.
  - `DeleteFileUploadTokenAsync(string uploadId)`: Deletes an upload record.
- **Dependencies**: `BitlinkDbContext`.
- **Flow**: Injected into `UploadService`.

#### File: Bitlink.Api/Repositories/BucketRepository.cs
- **Purpose**: S3-compatible object storage repository wrapping `AWSSDK.S3` for MinIO transfers.
- **Class**: `BucketRepository : IBucketRepository`
- **Methods**:
  - `DetermineProtocol()`: Inspects `S3Options.Endpoint` to assign `Protocol.HTTP` or `Protocol.HTTPS` dynamically so plain HTTP local MinIO endpoints do not cause SSL handshake errors.
  - `GetUploadSignedUrlAsync(string key, string contentType, long contentLength, int expiresIn)`: Generates a presigned S3 PUT URL for direct-to-storage client uploads with correct scheme.
  - `GetDownloadSignedUrlAsync(string key, int expiresIn)`: Generates a presigned S3 GET URL for file download/preview with correct scheme.
  - `FileExistsAsync(string key)`: Executes `GetObjectMetadataAsync` (HEAD request) to verify binary presence in MinIO.
  - `DeleteFileAsync(string key)`: Deletes the binary object from the bucket.
- **Dependencies**: `IAmazonS3`, `IOptions<S3Options>`.
- **Flow**: Injected into `UploadService` and `FileService`.

---


3. [Repositories](#3-repositories)
4. [Services](#4-services)
5. [Controllers & Hubs](#5-controllers--hubs)
### Section 4: Services

#### File: Bitlink.Api/Services/AuthService.cs
- **Purpose**: Orchestrates OAuth 2.0 PKCE token exchanges, user resource fetching, token refresh rotation with PostgreSQL advisory locking, and session invalidation.
- **Class**: `AuthService : IAuthService`
- **Methods**:
  - `ExchangeCodeAsync(string code, string codeVerifier)`: Sends POST `/tokenize` to Truth Auth Server, persists the session in PostgreSQL, fetches the user resource, and returns session ID + profile.
  - `FetchUserResourceAsync(string accessToken)`: Sends GET `/resource` with Bearer token to Truth Resource Server.
  - `RefreshTokensAsync(string sessionId, string refreshToken)`:
    - Initiates a PostgreSQL transaction.
    - Acquires advisory lock `pg_advisory_xact_lock(hashtext('refresh:' || sessionId))`.
    - Re-reads session: if already refreshed by another concurrent thread, commits and returns immediately.
    - Otherwise, sends POST `/refresh` to Truth Auth Server, updates the session in PostgreSQL, commits transaction (releasing the lock), and returns the updated session.
  - `LogoutAsync(string sessionId)`: Marks the session as revoked in PostgreSQL.
- **Dependencies**: `HttpClient`, `ISessionRepository`, `IOptions<AuthServerOptions>`.
- **Flow**: Injected into `AuthController` and `SessionService`.

#### File: Bitlink.Api/Services/SessionService.cs
- **Purpose**: Validates session cookies, verifies refresh-token expiration, and coordinates automatic access-token rotation.
- **Class**: `SessionService : ISessionService`
- **Methods**:
  - `ValidateSessionTokenAsync(string? token)`:
    - Validates token format (`session_` prefix).
    - Fetches active session from PostgreSQL.
    - If refresh token expired (>30 days), revokes session and throws 401.
    - If access token near expiration (<10s remaining), triggers `RefreshTokensAsync`.
    - Returns active, valid session entity.
- **Dependencies**: `ISessionRepository`, `IAuthService`.
- **Flow**: Injected into `SessionAuthAttribute` and `FileHub`.

#### File: Bitlink.Api/Services/UploadService.cs
- **Purpose**: Manages file upload authorization, presigned URL creation, cancellation, and confirmation.
- **Class**: `UploadService : IUploadService`
- **Methods**:
  - `RequestingToUploadAsync(RequestUploadInput data, string userId)`: Generates upload token, file ID, S3 PUT presigned URL, and records pending upload in PostgreSQL.
  - `UploadFailedAsync(string uploadId)`: Cleans up pending upload record and purges partial S3 binary.
  - `UploadSuccessAsync(string uploadId)`: Verifies object exists in S3, creates permanent file record in PostgreSQL, and removes temporary upload token.
- **Dependencies**: `IBucketRepository`, `IUploadRepository`, `IFileRepository`.
- **Flow**: Injected into `FileHub`.

#### File: Bitlink.Api/Services/FileService.cs
- **Purpose**: User file querying, ownership verification, download presigned URLs, file renaming, and file deletion.
- **Class**: `FileService : IFileService`
- **Methods**:
  - `ViewFilesAsync(ViewFilesInput query, string userId)`: Fetches paginated file list for the authenticated user.
  - `RequestingToDownloadAsync(string fileId, string userId)`: Verifies ownership, checks S3 object presence, and generates presigned GET download URL (60s TTL).
  - `RenameFileAsync(RenameFileInput data, string userId)`: Verifies ownership, checks S3 object presence, and updates file name in PostgreSQL.
  - `DeleteFileAsync(DeleteFileInput data, string userId)`: Verifies ownership, deletes file record from PostgreSQL, and deletes binary from S3.
- **Dependencies**: `IFileRepository`, `IBucketRepository`.
- **Flow**: Injected into `FileHub`.

#### File: Bitlink.Api/Services/CryptoService.cs
- **Purpose**: Cryptographically secure entity identifier generator with designated prefixes.
- **Class**: `CryptoService` (static)
- **Methods**:
  - `GenerateSessionId()`: Returns `"session_"` + 64 random hex characters.
  - `GenerateFileId()`: Returns `"file_"` + 64 random hex characters.
  - `GenerateUploadId()`: Returns `"upload_"` + 32 random hex characters.
  - `GenerateLockId()`: Returns `"lock_"` + 64 random hex characters.

---

### Section 5: Controllers & Hubs

#### File: Bitlink.Api/Controllers/AuthController.cs
- **Purpose**: REST HTTP controller handling OAuth authorization code exchanges, user profile retrieval, session invalidation, and health checks.
- **Class**: `AuthController : ControllerBase`
- **Methods**:
  - `ExchangeToken([FromBody] TokenizeRequest request)`: Validates code format (`auth_` prefix) and code_verifier length (43-128 chars), executes PKCE exchange with Truth, sets `sessionToken` HTTP-only cookie (30-day max age, SameSite=Lax), and returns user name and email.
  - `Me()`: Protected endpoint decorated with `[SessionAuth]`. Extracts access token from HttpContext items and returns current authenticated user profile.
  - `Logout()`: Extracts `sessionToken` cookie, revokes session in PostgreSQL, deletes cookie, and returns success response.
  - `ProtectedExample()`: Verification endpoint checking session authentication.
  - `Health()`: Service health check endpoint.
  - `Root()`: Fallback root route.
- **Dependencies**: `IAuthService`, `IWebHostEnvironment`.

#### File: Bitlink.Api/Hubs/FileHub.cs
- **Purpose**: Real-time SignalR communication hub replacing Socket.IO routes and room handlers.
- **Class**: `FileHub : Hub`
- **Methods**:
  - `OnConnectedAsync()`: Authenticates client by reading `sessionToken` cookie from HTTP handshake, validates session, joins `room_{userId}` group, and stores user metadata in Hub connection items.
  - `ViewFiles(ViewFilesInput data)`: Returns paginated file records for the caller.
  - `RequestingToUpload(RequestUploadInput data)`: Authorizes an upload, generating S3 presigned PUT URL.
  - `UploadSuccess(UploadAcknowledgementInput data)`: Finalizes file creation, returns created file, and broadcasts `fileUpdated` (`fileAdded`) to other clients in `room_{userId}`.
  - `UploadFailed(UploadAcknowledgementInput data)`: Aborts upload and deletes partial binary.
  - `RequestingToDownload(RequestDownloadInput data)`: Returns presigned GET download URL.
  - `RenameFile(RenameFileInput data)`: Renames file and broadcasts `fileUpdated` (`fileRenamed`) to other clients in `room_{userId}`.
  - `DeleteFile(DeleteFileInput data)`: Permanently deletes file and broadcasts `fileUpdated` (`fileDeleted`) to other clients in `room_{userId}`.
- **Dependencies**: `ISessionService`, `IFileService`, `IUploadService`.

---

### Section 6: Middlewares & Validation

#### File: Bitlink.Api/Middleware/SessionAuthAttribute.cs
- **Purpose**: Action filter enforcing valid session cookie authentication on protected endpoints.
- **Class**: `SessionAuthAttribute : Attribute, IAsyncActionFilter`
- **Flow**: Reads `sessionToken` cookie, delegates validation to `ISessionService`, and injects `SessionId`, `AccessToken`, and `UserId` into `HttpContext.Items`.

#### File: Bitlink.Api/Middleware/ExceptionHandlingMiddleware.cs
- **Purpose**: Intercepts unhandled exceptions and `BitlinkException`, converting them into uniform JSON responses `{ success: false, error: message }` with corresponding status codes.
- **Class**: `ExceptionHandlingMiddleware`

---

### Section 7: Models & Utilities

#### File: Bitlink.Api/Models/StandardResponse.cs
- **Purpose**: Universal response DTO enforcing standard response shapes: `{ success: true, message: "...", data: ... }` and `{ success: false, error: "..." }`.

#### File: Bitlink.Api/Models/AuthModels.cs
- **Purpose**: Data transfer objects for OAuth code exchange (`TokenizeRequest`), Truth token payloads (`AuthTokens`), and user profiles (`UserResource`).

#### File: Bitlink.Api/Models/FileModels.cs
- **Purpose**: Data transfer objects for file listing, upload requests, downloads, renaming, and deletion.

#### File: Bitlink.Api/Utils/ApplicationError.cs
- **Purpose**: Custom `BitlinkException` exception class and static `ValidationHelpers.StrictCheck` methods guaranteeing non-nullability and business invariants.

