# Truth Coding Style Guide & Conventions

This guide documents the exact coding habits, patterns, and conventions across Truth. All new code or modifications must adhere to these standards to ensure seamless consistency with the codebase.

---

## 1. File & Module Structure

### A. Section Separator Headers
In entry point and configuration files (`server.ts`, etc.), organize code into clear visual sections using hyphenated banners:
```ts
// ------------- Imports ---------------
// ------------- Configuration ---------------
// ------------- Database Connection ---------------
// ------------- Indexes ---------------
// ------------- Instances ---------------
// ------------- Routes ---------------
// ------------- Server Start ---------------
// ------------- Shutdown ---------------
```

### B. ES Module Imports with `.js` Extensions
- In backend TypeScript source files, relative imports must explicitly include the `.js` extension to maintain Node ESM compatibility:
```ts
import AuthService from "../services/AuthService.js";
import { strictCheck, errorHandling } from "../utils/ApplicationError.js";
```
- For type-only imports, always use explicit `import type`:
```ts
import type { RequestHandler, Response } from "express";
import type { User, Session } from "../types/dbSchema.js";
```

---

## 2. JSDoc Documentation Standards & Inorder Flow Alignment
- Every controller, service, repository, and middleware method must include a JSDoc block structured with an **Inorder Flow** checklist and optional `@remarks` or `@refinement`.
- **1-to-1 Step & Empty Line Rule**: In the method implementation, each individual code block corresponding to a step in the `Inorder Flow` checklist **must be separated by an empty line**.

```ts
/**
 * Handles user login Request
 * 
 * Inorder Flow:
 * - Retrieve email, password, ip, and hardware from request body and headers
 * - Verify user credentials and generate session
 * - Set HTTP-only cookie with sessionId
 * - Structure and send response
 */
userLogin: RequestHandler = async (req, res) => {
    try {
        const { email, password }: { email: string; password: string } = req.body;
        const ip = req.ip as string;
        const userAgent = req.headers["user-agent"] as string;
        const hardware = this.deviceDetector.parse(userAgent) as Hardware;

        const loginPayload = strictCheck(await this.authService.userLogin({ email, password, ip, hardware }), 401, "Invalid Credentials");

        res.cookie("authToken", loginPayload.sessionId, {
            httpOnly: true,
            secure: false,
            sameSite: "lax",
            maxAge: 4320 * 60 * 1000,
        });

        res.status(200).json({
            success: true,
            message: "User has been logged Successfully",
            data: {
                name: loginPayload.name,
                email: loginPayload.email
            }
        });
    } catch (error) {
        errorHandling(error, res);
    }
};
```

---

## 3. Backend Architecture & Coding Patterns

### A. Layered Dependency Injection
- Architecture is strictly split into **Controllers -> Services -> Repositories**.
- Use ES6 classes with constructor-injected dependencies:
```ts
export default class UserController {
    private userService: UserService;

    constructor(userService: UserService) {
        this.userService = userService;
    }
}
```

### B. Controller Method Signatures & Error Wrapping
- Express controller handlers are defined as class arrow properties typed with `RequestHandler`:
```ts
getUserProfile: RequestHandler = async (req, res) => {
    try {
        const userId = strictCheck(req.user?.id, 500, "Internal Server Error");
        const profile = await this.userService.getProfile(userId);

        res.status(200).json({
            success: true,
            message: "User profile retrieved successfully",
            data: profile
        });
    } catch (error) {
        errorHandling(error, res);
    }
};
```

### C. Assertions & Error Handling
- Never perform silent null/undefined ignores or return raw unhandled errors to clients.
- Use `strictCheck(value, statusCode, message)` to guarantee non-nullable types and enforce HTTP status codes:
```ts
const user = strictCheck(await this.userRepository.getUserById(id), 404, "User does not exist");
```
- Throw `ApplicationError(statusCode, message)` for business logic violations.
- In controllers, delegate caught exceptions to `errorHandling(error, res)`.

### D. Consistent Standard JSON Response Structure
All API responses must follow this uniform shape:
```json
{
  "success": true,
  "message": "Human-readable description of result",
  "data": { ... }
}
```
For error responses:
```json
{
  "success": false,
  "error": "Error description"
}
```

### E. Entity ID Prefixes
All generated IDs must use the dedicated static helpers in `CryptoService`:
- `user_` + 16 random hex bytes
- `org_` + 16 random hex bytes
- `client_` + 16 random hex bytes
- `secret_` + 16 random hex bytes
- `session_` + 32 random hex bytes
- `auth_` + 16 random hex bytes
- `at_` + 32 random hex bytes
- `rt_` + 48 random hex bytes
- `mt_` + 16 random hex bytes

---

## 4. Frontend Architecture & Coding Patterns

### A. React & TypeScript Conventions
- Use functional React components with explicit prop interfaces.
- Prefer PascalCase for component filenames and folders (`AuthPages.tsx`, `PortalLayout.tsx`).
- Route definitions and API endpoints must never be hardcoded in components; import from `@/config/routes`.

### B. Forms & Validation
- Standardize on `react-hook-form` paired with `@hookform/resolvers/zod` and centralized Zod schemas from `@/types/validation`:
```tsx
const { register, handleSubmit, formState: { errors, isSubmitting } } = useForm<LoginSchema>({
    resolver: zodResolver(LoginSchema),
    defaultValues: { email: "", password: "" }
});
```

### C. API Interactions
- Utilize the `useApiService` custom hook or Context actions (`useAuth()`, `useNotification()`).
- Always show loading indicators (`Spinner`, `LoaderCircleIcon`) during asynchronous actions and disable submit buttons.

