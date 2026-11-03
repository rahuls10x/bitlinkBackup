# Truth — OAuth 2.0 Integration Guide

> **Version**: 1.0  
> **Base URL**: `http://localhost:4000`  
> **Protocol**: HTTP/HTTPS REST  
> **Content-Type**: `application/json`

This guide provides everything an external developer needs to integrate with the **Truth Authentication Server** using the OAuth 2.0 Authorization Code Grant with PKCE.

---

## Table of Contents

- [Overview](#overview)
- [Prerequisites](#prerequisites)
- [Supported Scopes](#supported-scopes)
- [PKCE Reference](#pkce-reference)
- [Token Lifetimes](#token-lifetimes)
- [Validation Constraints](#validation-constraints)
- [Standard Response Format](#standard-response-format)
- [OAuth 2.0 Flow Overview](#oauth-20-flow-overview)
- [Step 1 — Authorization Request](#step-1--authorization-request)
- [Step 2 — User Consent](#step-2--user-consent)
- [Step 3 — Token Exchange](#step-3--token-exchange)
- [Step 4 — Using the Access Token](#step-4--using-the-access-token)
- [Step 5 — Refreshing Tokens](#step-5--refreshing-tokens)
- [Complete Flow Sequence Diagram](#complete-flow-sequence-diagram)
- [Error Reference](#error-reference)

---

## Overview

Truth is an **OAuth 2.0 Authorization Server** that supports the **Authorization Code Grant with PKCE** (Proof Key for Code Exchange). It issues opaque access and refresh tokens to authorized client applications on behalf of authenticated users.

**Supported Grant Type**: Authorization Code with PKCE  
**Supported Response Type**: `code`  
**Supported Challenge Method**: `S256`

---

## Prerequisites

Before integrating, you need the following:

| Requirement | Description |
|---|---|
| **Organization Account** | Register an organization at the Truth portal to manage your client applications. |
| **Client Application** | Create a client application under your organization to receive your credentials. |
| **Client ID** | Issued on client creation. Prefixed with `client_` (e.g., `client_a1b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6`). |
| **Client Secret** | Issued on client creation. Prefixed with `secret_` (e.g., `secret_a1b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6`). Keep this **confidential** — never expose it in client-side code. |
| **Redirect URI** | The callback URL registered with your client application. Must **exactly match** the value registered. |
| **Scopes** | The set of user data permissions your application requests. |

> ⚠️ **Important**: Your `client_secret` is a **server-side credential**. Never embed it in frontend JavaScript, mobile app binaries, or any publicly accessible code. Token exchange and refresh must always happen from your **backend server**.

---

## Supported Scopes

When initiating an authorization request, you may request any combination of the following scopes:

| Scope | Description |
|---|---|
| `name` | User's display name |
| `email` | User's email address |
| `age` | User's age |
| `gender` | User's gender |
| `dob` | User's date of birth |
| `avatar` | User's avatar / profile picture |

Scopes are space-delimited in the authorization request query string and array-delimited in request bodies.

```
scope=name email age
```

---

## PKCE Reference

Truth **requires** PKCE (Proof Key for Code Exchange) on every authorization request. Only the `S256` challenge method is supported.

### Generating the Code Verifier

The `code_verifier` is a cryptographically random string between **43 and 128 characters** (inclusive).

```javascript
const crypto = require("crypto");

const codeVerifier = crypto.randomBytes(32).toString("base64url");
// Example: "dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk"
```

### Deriving the Code Challenge

The `code_challenge` is the **Base64-URL-encoded SHA-256 hash** of the `code_verifier`.

```javascript
const codeChallenge = crypto
  .createHash("sha256")
  .update(codeVerifier)
  .digest("base64url");
// Example: "E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM"
```

> 📌 **Note**: Only `S256` is supported. Plain code challenges are **not accepted**.

---

## Token Lifetimes

| Token / Artifact | Lifetime | Notes |
|---|---|---|
| Authorization Code | **40 seconds** | Single-use. Consumed on token exchange. |
| Access Token | **1 hour** | Used as Bearer token for resource access. |
| Refresh Token | **30 days** | Single-use rotation. Old token is deactivated on refresh. |

---

## Validation Constraints

All inputs are validated before processing. Requests that fail validation receive a `400` status with an error message.

| Field | Constraint |
|---|---|
| `client_id` | String. Must start with `client_`. |
| `client_secret` | String. Must start with `secret_`. |
| `code` (authorization code) | String. Must start with `auth_`. |
| `refresh_token` | String. Must start with `rt_`. |
| `code_verifier` | String. Minimum 43 characters, maximum 128 characters. |
| `redirect_uri` | Must be a valid URL. |
| `response_type` | Must be exactly `code`. |
| `challenge_method` | Must be exactly `S256`. |
| `scope` | Space-separated string (in query) or array (in body). Each value must be one of: `name`, `email`, `age`, `gender`, `dob`, `avatar`. |
| `state` | Non-empty string. |
| `consent` | Boolean (`true` or `false`). |

---

## Standard Response Format

All JSON responses from the authentication server follow a uniform structure.

### Success Response

```json
{
  "success": true,
  "message": "Human-readable description of the result",
  "data": { }
}
```

### Error Response

```json
{
  "success": false,
  "error": "Human-readable error description"
}
```

> 📌 **Note**: OAuth authorization errors on the `/authorize` endpoint do **not** return JSON. They redirect to the consent page with `error` and `error_description` query parameters instead. See [Step 1](#step-1--authorization-request) for details.

---

## OAuth 2.0 Flow Overview

The complete OAuth flow follows these steps:

```
1. Your app redirects the user to Truth's /authorize endpoint
2. The user logs in (if not already authenticated) and approves consent
3. Truth redirects back to your redirect_uri with an authorization code
4. Your backend exchanges the code for tokens via POST /tokenize
5. Use the access token to call protected resources
6. When the access token expires, use GET /refresh to get new tokens
```

---

## Step 1 — Authorization Request

Redirect the user's browser to the authorization endpoint to begin the OAuth flow.

### Endpoint

```http
GET /authorize
```

### Query Parameters

| Parameter | Required | Description |
|---|---|---|
| `client_id` | ✅ | Your application's client ID (starts with `client_`). |
| `redirect_uri` | ✅ | Your registered callback URL. Must exactly match the registered value. |
| `response_type` | ✅ | Must be `code`. |
| `scope` | ✅ | Space-separated list of requested scopes (e.g., `name email age`). |
| `state` | ✅ | An opaque string your app generates to prevent CSRF. Returned unchanged in the callback. |
| `code_challenge` | ✅ | The Base64-URL-encoded SHA-256 hash of your `code_verifier`. |
| `challenge_method` | ✅ | Must be `S256`. |

### Example Request URL

```
http://localhost:4000/authorize?client_id=client_abc123&redirect_uri=http%3A%2F%2Flocalhost%3A3000%2Fcallback&response_type=code&scope=name%20email%20age&state=xyzSecureRandom&code_challenge=E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM&challenge_method=S256
```

### What Happens Next

The authorization endpoint evaluates the request through a series of checks. The outcome depends on the user's authentication state and prior consent:

#### Scenario A — User is Not Authenticated

If the user does **not** have a valid session cookie (`authToken`), Truth redirects them to the login page with a `next` parameter that preserves the full authorization URL:

```http
HTTP/1.1 302 Found
Location: http://localhost:5173/user/login?next=http%3A%2F%2Flocalhost%3A4000%2Fauthorize%3Fclient_id%3D...
```

After the user logs in successfully, they are automatically redirected back to the `/authorize` endpoint to resume the flow.

#### Scenario B — User Has Prior Consent (Matching Scopes)

If the user is authenticated **and** has already granted consent for this `client_id` with the exact same scopes, Truth issues an authorization code immediately and redirects to your `redirect_uri`:

```http
HTTP/1.1 302 Found
Location: http://localhost:3000/callback?response_type=code&state=xyzSecureRandom&code=auth_a1b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6&expires_in=40
```

#### Scenario C — User Has No Prior Consent (or Scope Mismatch)

If the user is authenticated but has **not** previously consented (or the requested scopes differ from the stored consent), Truth redirects the user to the **Consent Page** on the frontend portal:

```http
HTTP/1.1 302 Found
Location: http://localhost:5173/consent?client_id=client_abc123&redirect_uri=...&response_type=code&state=xyzSecureRandom&scope=name%20email%20age&challenge_method=S256&code_challenge=E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM&client_name=MyApp
```

The user reviews the requested scopes and either approves or denies. See [Step 2](#step-2--user-consent) for what happens next.

### Authorization Success Callback

On success, the user is redirected to your `redirect_uri` with the following query parameters:

| Parameter | Description |
|---|---|
| `response_type` | Echoes `code`. |
| `state` | The exact `state` value you provided. **Verify this matches** to prevent CSRF. |
| `code` | The one-time authorization code (prefixed with `auth_`). Valid for **40 seconds**. |
| `expires_in` | Code lifetime in seconds (`40`). |

```http
HTTP/1.1 302 Found
Location: http://localhost:3000/callback?response_type=code&state=xyzSecureRandom&code=auth_a1b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6&expires_in=40
```

### Authorization Error Redirects

If the authorization request fails business logic validation, Truth redirects to the **consent page** with `error` and `error_description` query parameters. These are **not** JSON responses — they are redirect-based errors following the OAuth 2.0 specification pattern.

```http
HTTP/1.1 302 Found
Location: http://localhost:5173/consent?error=invalid_client&error_description=Client%20not%20found
```

| Error Code | `error_description` | Cause |
|---|---|---|
| `invalid_client` | `Client not found` | The provided `client_id` does not match any registered client. |
| `invalid_request` | `Invalid redirect uri` | The provided `redirect_uri` does not match the registered value. |
| `unsupported_response_type` | `Invalid response type` | The provided `response_type` is not `code`. |
| `server_error` | `Failed to create authorization code` | Internal error while generating the authorization code. |

> ⚠️ **Validation errors** (missing or malformed query parameters) return standard JSON error responses with HTTP `400` **before** reaching the authorization logic:
>
> ```json
> {
>   "success": false,
>   "error": "Some or all required Query parameters are missing"
> }
> ```

---

## Step 2 — User Consent

When the user is redirected to the consent page (Scenario C above), they see the application name, the requested scopes, and an approve/deny prompt. Your application does **not** call this endpoint directly — the Truth frontend portal handles it. This section is provided for completeness.

### Endpoint

```http
POST /consent
```

### Authentication

Requires a valid **user** session cookie (`authToken`). Organization sessions are not accepted.

### Request Body

```json
{
  "client_id": "client_a1b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6",
  "scopes": ["name", "email", "age"],
  "consent": true
}
```

| Field | Type | Required | Description |
|---|---|---|---|
| `client_id` | `string` | ✅ | The client application ID (starts with `client_`). |
| `scopes` | `string[]` | ✅ | Array of approved scopes. |
| `consent` | `boolean` | ✅ | `true` to approve, `false` to deny. |

### Success Response — `201 Created`

```json
{
  "success": true,
  "message": "Consent has been noted Successfully"
}
```

### Error Responses

| Status | Error Message | Cause |
|---|---|---|
| `400` | `Client not found` | The `client_id` does not match any registered client. |
| `401` | `Unauthorized` | No session cookie present. |
| `401` | `Session does not exist` | Session cookie is invalid or expired. |
| `500` | `Internal Server Error` | Failed to create or update the consent record. |

> 📌 **After consent is approved**, the user must re-initiate the authorization request (redirect to `/authorize` again). This time, the existing consent will be found and an authorization code will be issued directly (Scenario B).

---

## Step 3 — Token Exchange

After receiving the authorization code at your `redirect_uri`, your **backend server** exchanges it for an access token and refresh token.

### Endpoint

```http
POST /tokenize
```

### Authentication

**None** — this endpoint uses client credentials in the request body instead.

### Request Body

```json
{
  "client_id": "client_a1b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6",
  "client_secret": "secret_a1b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6",
  "code": "auth_a1b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6",
  "code_verifier": "dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk"
}
```

| Field | Type | Required | Description |
|---|---|---|---|
| `client_id` | `string` | ✅ | Your client application ID. |
| `client_secret` | `string` | ✅ | Your client application secret. |
| `code` | `string` | ✅ | The authorization code received in the callback (starts with `auth_`). |
| `code_verifier` | `string` | ✅ | The original PKCE code verifier (43–128 characters). |

### Success Response — `200 OK`

```json
{
  "success": true,
  "message": "Token has been issued Successfully",
  "data": {
    "access_token": "at_a1b2c3d4e5f6a7b8...",
    "token_type": "Bearer",
    "expires_in": 3600000,
    "refresh_token": "rt_a1b2c3d4e5f6a7b8...",
    "scopes": ["name", "email", "age"]
  }
}
```

| Field | Type | Description |
|---|---|---|
| `access_token` | `string` | Opaque access token (prefixed with `at_`). Use as a Bearer token. |
| `token_type` | `string` | Always `"Bearer"`. |
| `expires_in` | `number` | Token lifetime in **milliseconds** (`3600000` = 1 hour). |
| `refresh_token` | `string` | Opaque refresh token (prefixed with `rt_`). Use to obtain new tokens. |
| `scopes` | `string[]` | The scopes granted for this token. |

### Error Responses

| Status | Error Message | Cause |
|---|---|---|
| `400` | `Authorization code is invalid or expired` | Code already used, does not exist, or expired (40s TTL). |
| `400` | `Unauthorized client id` | `client_id` does not match the code's bound client. |
| `400` | `Client not found` | `client_id` does not correspond to any registered application. |
| `400` | `Unauthorized client secret` | `client_secret` does not match the registered secret. |
| `400` | `Code verifier does not match to code challenge` | PKCE verification failed — SHA-256 mismatch. |
| `500` | `Failed to cache access token` | Internal error while caching the access token. |
| `500` | `Failed to create access token` | Internal error while persisting the access token. |
| `500` | `Failed to create refresh token` | Internal error while persisting the refresh token. |

> ⚠️ **The authorization code is single-use.** It is consumed (deleted) the moment the token exchange request reads it. If token creation fails after the code is consumed, you must restart the authorization flow from [Step 1](#step-1--authorization-request).

---

## Step 4 — Using the Access Token

Once you have an access token, include it in the `Authorization` header when calling protected resource endpoints.

### Header Format

```http
Authorization: Bearer at_a1b2c3d4e5f6a7b8...
```

### Example Request

```bash
curl -X GET http://localhost:5000/resource \
  -H "Authorization: Bearer at_a1b2c3d4e5f6a7b8..."
```

> 📌 **Note**: The resource server runs on a separate port (`5000`). The specific resource endpoints and their responses are outside the scope of this guide.

---

## Step 5 — Refreshing Tokens

When the access token expires (after 1 hour), use the refresh token to obtain a new token pair. This must be done from your **backend server**.

### Endpoint

```http
POST /refresh
```

### Request Body

```json
{
  "refresh_token": "rt_a1b2c3d4e5f6a7b8...",
  "client_id": "client_a1b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6",
  "client_secret": "secret_a1b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6"
}
```

| Field | Type | Required | Description |
|---|---|---|---|
| `refresh_token` | `string` | ✅ | The refresh token from the previous token response (starts with `rt_`). |
| `client_id` | `string` | ✅ | Your client application ID. |
| `client_secret` | `string` | ✅ | Your client application secret. |

### Success Response — `200 OK`

```json
{
  "success": true,
  "message": "Token has been refreshed Successfully",
  "data": {
    "access_token": "at_new_access_token...",
    "token_type": "Bearer",
    "expires_in": 3600000,
    "refresh_token": "rt_new_refresh_token...",
    "scopes": ["name", "email", "age"]
  }
}
```

The response shape is **identical** to the [Token Exchange](#step-3--token-exchange) success response.

### Token Rotation

Truth implements **refresh token rotation**:

- Each time you use a refresh token, a **new** access token **and** a **new** refresh token are issued.
- The **old refresh token is permanently deactivated** and cannot be used again.
- You must store and use the **new** refresh token for subsequent refresh requests.

> ⚠️ **Store the new refresh token immediately.** If you discard it or fail to persist it, you will lose the ability to refresh and must restart the full authorization flow.

### Error Responses

| Status | Error Message | Cause |
|---|---|---|
| `400` | `Refresh token is invalid or expired` | Token does not exist, was already rotated, or expired (30-day TTL). |
| `400` | `Unauthorized client id` | `client_id` does not match the refresh token's bound client. |
| `400` | `Client not found` | `client_id` does not correspond to any registered application. |
| `400` | `Unauthorized client secret` | `client_secret` does not match the registered secret. |
| `400` | `Failed to delete refresh token` | Internal error deactivating the used refresh token. |
| `500` | `Failed to cache access token` | Internal error caching the new access token. |
| `500` | `Failed to create access token` | Internal error persisting the new access token. |
| `500` | `Failed to create refresh token` | Internal error persisting the new refresh token. |

---

## Complete Flow Sequence Diagram

```text
┌──────────┐         ┌───────────┐        ┌──────────────┐        ┌────────────┐
│  User's  │         │   Your    │        │    Truth     │        │   Truth    │
│ Browser  │         │  Backend  │        │  Auth Server │        │  Frontend  │
└────┬─────┘         └─────┬─────┘        └──────┬───────┘        └─────┬──────┘
     │                     │                     │                      │
     │  1. Click "Login    │                     │                      │
     │     with Truth"     │                     │                      │
     │────────────────────>│                     │                      │
     │                     │  2. Generate        │                      │
     │                     │     code_verifier   │                      │
     │                     │     code_challenge  │                      │
     │                     │     state           │                      │
     │  3. 302 to /authorize                     │                      │
     │<────────────────────│                     │                      │
     │  4. GET /authorize?client_id=...&code_challenge=...              │
     │──────────────────────────────────────────>│                      │
     │                     │           ┌─────────┴──────────┐           │
     │                     │           │ Is user logged in? │           │
     │                     │           └─────────┬──────────┘           │
     │                     │              No ────┤                      │
     │  5a. 302 to /user/login?next=...          │                      │
     │<──────────────────────────────────────────│                      │
     │  5b. User logs in via frontend            │                      │
     │─────────────────────────────────────────────────────────────────>│
     │  5c. Auto-redirect back to /authorize     │                      │
     │──────────────────────────────────────────>│                      │
     │                     │           ┌─────────┴──────────┐           │
     │                     │           │ Has prior consent? │           │
     │                     │           └─────────┬──────────┘           │
     │                     │              No ────┤                      │
     │  6a. 302 to /consent page                 │                      │
     │<──────────────────────────────────────────│                      │
     │  6b. User approves scopes                 │                      │
     │──────────────────────────────────────────>│  POST /consent       │
     │  6c. Re-initiate /authorize               │                      │
     │──────────────────────────────────────────>│                      │
     │                     │             Yes ────┤                      │
     │  7. 302 to redirect_uri?code=auth_...&state=...                  │
     │<──────────────────────────────────────────│                      │
     │  8. Forward code    │                     │                      │
     │     to backend      │                     │                      │
     │────────────────────>│                     │                      │
     │                     │  9. POST /tokenize  │                      │
     │                     │     {code, verifier, │                      │
     │                     │      id, secret}    │                      │
     │                     │────────────────────>│                      │
     │                     │  10. {access_token, │                      │
     │                     │       refresh_token}│                      │
     │                     │<────────────────────│                      │
     │  11. Authenticated! │                     │                      │
     │<────────────────────│                     │                      │
     │        ...later, access token expires...  │                      │
     │                     │  12. POST /refresh   │                      │
     │                     │     {refresh_token, │                      │
     │                     │      id, secret}    │                      │
     │                     │────────────────────>│                      │
     │                     │  13. {new_access,   │                      │
     │                     │       new_refresh}  │                      │
     │                     │<────────────────────│                      │
```

---

## Error Reference

A consolidated reference of all possible error responses across the OAuth endpoints.

### Validation Errors (All Endpoints)

These errors occur when the request payload fails input validation **before** reaching business logic.

| Status | Error Message | Cause |
|---|---|---|
| `400` | `Invalid Data` | Request body is empty or missing. |
| `400` | `Some or all required fields are missing` | One or more required body fields are absent. |
| `400` | `Some or all required Query parameters are missing` | One or more required query parameters are absent (applies to `GET /authorize`). |
| `400` | `Client Id must be a string` | `client_id` is not a string. |
| `400` | `Client Secret must be a string` | `client_secret` is not a string. |
| `400` | `Authorization Code must be a string` | `code` is not a string. |
| `400` | `Refresh Token must be a string` | `refresh_token` is not a string. |
| `400` | `Code Verifier must be a string` | `code_verifier` is not a string. |
| `400` | `Invalid Code Verifier.` | `code_verifier` length is outside the 43–128 character range. |
| `400` | `Invalid Response Type.` | `response_type` is not `code`. |
| `400` | `Invalid Challenge Method.` | `challenge_method` is not `S256`. |
| `400` | `Invalid Scope.` | One or more scope values are not in the allowed set. |

### Authorization Errors (`GET /authorize`)

These are **redirect-based** errors — the browser is redirected with `error` and `error_description` query parameters.

| Error Code | Description | Cause |
|---|---|---|
| `invalid_client` | `Client not found` | No registered client matches the `client_id`. |
| `invalid_request` | `Invalid redirect uri` | `redirect_uri` does not match the registered value. |
| `unsupported_response_type` | `Invalid response type` | `response_type` does not match the client's registered type. |
| `server_error` | `Failed to create authorization code` | Internal error during code generation. |

### Consent Errors (`POST /consent`)

| Status | Error Message | Cause |
|---|---|---|
| `400` | `Client not found` | The `client_id` does not match any registered client. |
| `401` | `Unauthorized` | Missing session cookie. |
| `401` | `Session does not exist` | Invalid or expired session. |
| `500` | `Failed to create user consent` | Failed to persist new consent record. |
| `500` | `Failed to update user consent` | Failed to update existing consent record. |

### Token Exchange Errors (`POST /tokenize`)

| Status | Error Message | Cause |
|---|---|---|
| `400` | `Authorization code is invalid or expired` | Code does not exist, was already used, or TTL (40s) elapsed. |
| `400` | `Unauthorized client id` | `client_id` mismatch with the code's bound client. |
| `400` | `Client not found` | `client_id` does not exist. |
| `400` | `Unauthorized client secret` | `client_secret` does not match. |
| `400` | `Code verifier does not match to code challenge` | PKCE verification failed (SHA-256 mismatch). |
| `500` | `Failed to cache access token` | Redis caching error. |
| `500` | `Failed to create access token` | Database persistence error for access token. |
| `500` | `Failed to create refresh token` | Database persistence error for refresh token. |

### Token Refresh Errors (`POST /refresh`)

| Status | Error Message | Cause |
|---|---|---|
| `400` | `Refresh token is invalid or expired` | Token does not exist, was already rotated, or TTL (30 days) elapsed. |
| `400` | `Unauthorized client id` | `client_id` mismatch with the refresh token's bound client. |
| `400` | `Client not found` | `client_id` does not exist. |
| `400` | `Unauthorized client secret` | `client_secret` does not match. |
| `400` | `Failed to delete refresh token` | Failed to deactivate the used refresh token. |
| `500` | `Failed to cache access token` | Redis caching error during new token creation. |
| `500` | `Failed to create access token` | Database persistence error for new access token. |
| `500` | `Failed to create refresh token` | Database persistence error for new refresh token. |

---

> 📌 **Questions or Issues?** If you encounter unexpected behavior, review the error messages returned by the API — they are designed to be descriptive and actionable. Ensure your `redirect_uri`, `client_id`, and `client_secret` exactly match your registered application credentials.
