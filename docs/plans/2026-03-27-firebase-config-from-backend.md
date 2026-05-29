# Firebase Config from Backend Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use forge:executing-plans to implement this plan task-by-task.

**Goal:** Allow consumers of the LiteBase library to provide their Firebase configuration via C# options so the UI fetches it dynamically at startup instead of having it hardcoded.

**Architecture:** Add a `LiteBaseOptions` class holding `DbPath` and a nullable `FirebaseConfig`. `LiteBaseEndpointMiddleware` accepts `LiteBaseOptions` instead of a raw string and serves the Firebase config at `GET /litebase/config`. The Vue UI fetches this endpoint before mounting and errors hard if the config is missing.

**Tech Stack:** .NET 8 ASP.NET Core middleware, Vue 3, TypeScript, Firebase JS SDK v12

---

### Task 1: Add `LiteBaseOptions` and `FirebaseConfig` to the backend

**Files:**
- Modify: `LiteBase/LiteBase.cs`

**Step 1: Add the options classes**

At the top of `LiteBase.cs` (after the `using` statements, before `LiteBaseUIMiddleware`), add:

```csharp
public class LiteBaseOptions
{
    public required string DbPath { get; set; }
    public FirebaseConfig? Firebase { get; set; }
}

public class FirebaseConfig
{
    public required string ApiKey { get; set; }
    public required string AuthDomain { get; set; }
    public required string ProjectId { get; set; }
    public required string StorageBucket { get; set; }
    public required string MessagingSenderId { get; set; }
    public required string AppId { get; set; }
}
```

**Step 2: Update `LiteBaseEndpointMiddleware` to accept `LiteBaseOptions`**

Change the primary constructor from:
```csharp
public class LiteBaseEndpointMiddleware(RequestDelegate next, string dbPath)
```
to:
```csharp
public class LiteBaseEndpointMiddleware(RequestDelegate next, LiteBaseOptions options)
```

Replace all uses of `dbPath` with `options.DbPath`.

**Step 3: Add the `/litebase/config` endpoint**

Inside `LiteBaseEndpointMiddleware.Invoke`, add this block before the `/litebase/tables` check:

```csharp
if (context.Request.Path.Equals("/litebase/config", StringComparison.OrdinalIgnoreCase))
{
    context.Response.ContentType = "application/json";
    await context.Response.WriteAsJsonAsync(options.Firebase);
    return;
}
```

**Step 4: Update `UseLiteBaseEndpoint` extension method**

Change the signature from:
```csharp
public static IApplicationBuilder UseLiteBaseEndpoint(this IApplicationBuilder builder, string dbPath)
{
    return builder.UseMiddleware<LiteBaseEndpointMiddleware>(dbPath);
}
```
to:
```csharp
public static IApplicationBuilder UseLiteBaseEndpoint(this IApplicationBuilder builder, LiteBaseOptions options)
{
    return builder.UseMiddleware<LiteBaseEndpointMiddleware>(options);
}
```

**Step 5: Update `DemoApp/Program.cs` to use the new signature**

Change:
```csharp
app.UseLiteBaseEndpoint("db/blogging.db");
```
to:
```csharp
app.UseLiteBaseEndpoint(new LiteBaseOptions
{
    DbPath = "db/blogging.db",
    Firebase = new FirebaseConfig
    {
        ApiKey = "AIzaSyCqB7VHM1-t_3gOrpXKUIB2gMs4DqirrpA",
        AuthDomain = "bgquest.app",
        ProjectId = "boardgame-tracker-2498e",
        StorageBucket = "boardgame-tracker-2498e.firebasestorage.app",
        MessagingSenderId = "483970449590",
        AppId = "1:483970449590:web:07fe9000eaf7b4bbf32617"
    }
});
```

**Step 6: Build to verify no compile errors**

```bash
dotnet build SqlLiteUI.sln
```
Expected: `Build succeeded` with 0 errors.

**Step 7: Commit**

```bash
git add LiteBase/LiteBase.cs DemoApp/Program.cs
git commit -m "feat: add LiteBaseOptions and serve Firebase config from /litebase/config"
```

---

### Task 2: Fetch Firebase config dynamically in the UI

**Files:**
- Modify: `LiteBase/ui/src/firebase.ts`
- Modify: `LiteBase/ui/src/main.ts`

**Step 1: Rewrite `firebase.ts` to export an async init function**

Replace the entire contents of `LiteBase/ui/src/firebase.ts` with:

```typescript
import { initializeApp, type FirebaseApp } from 'firebase/app'
import { getAuth, type Auth } from 'firebase/auth'

interface FirebaseConfig {
  apiKey: string
  authDomain: string
  projectId: string
  storageBucket: string
  messagingSenderId: string
  appId: string
}

let app: FirebaseApp
let auth: Auth

export async function initFirebase(): Promise<void> {
  const response = await fetch('/litebase/config')
  const config: FirebaseConfig | null = await response.json()

  if (!config) {
    throw new Error('Firebase configuration is missing. Set Firebase in LiteBaseOptions on the server.')
  }

  app = initializeApp(config)
  auth = getAuth(app)
}

export function getFirebaseAuth(): Auth {
  if (!auth) throw new Error('Firebase has not been initialised. Call initFirebase() first.')
  return auth
}

export { app }
```

**Step 2: Update `main.ts` to call `initFirebase()` before mounting**

Replace the entire contents of `LiteBase/ui/src/main.ts` with:

```typescript
import { createApp } from 'vue'
import { VueQueryPlugin } from '@tanstack/vue-query'
import './index.css'
import App from './App.vue'
import PrimeVue from 'primevue/config'
import Aura from '@primeuix/themes/aura'
import { initFirebase } from './firebase'

async function bootstrap() {
  try {
    await initFirebase()
  } catch (e) {
    document.body.innerHTML = `<div style="display:flex;align-items:center;justify-content:center;height:100vh;font-family:sans-serif;color:#dc2626">
      <div style="text-align:center;max-width:480px">
        <h1 style="font-size:1.5rem;font-weight:700;margin-bottom:0.5rem">Configuration Error</h1>
        <p style="color:#6b7280">${(e as Error).message}</p>
      </div>
    </div>`
    return
  }

  const app = createApp(App)
  app.use(PrimeVue, {
    theme: {
      preset: Aura,
      options: {
        darkModeSelector: '.dark'
      }
    }
  })
  app.use(VueQueryPlugin)
  app.mount('#app')
}

bootstrap()
```

**Step 3: Update `App.vue` to use `getFirebaseAuth()` instead of the direct `auth` import**

In `LiteBase/ui/src/App.vue`, change the import line:
```typescript
import { auth } from './firebase'
```
to:
```typescript
import { getFirebaseAuth } from './firebase'
```

Then replace every usage of `auth` in the script with `getFirebaseAuth()`:
- `onAuthStateChanged(auth, ...)` → `onAuthStateChanged(getFirebaseAuth(), ...)`
- `signOut(auth)` → `signOut(getFirebaseAuth())`

**Step 4: Build the UI to verify no TypeScript errors**

```bash
cd LiteBase/ui && pnpm build
```
Expected: build completes with no errors, `dist/litebase.js` and `dist/litebase.css` updated.

**Step 5: Commit**

```bash
git add LiteBase/ui/src/firebase.ts LiteBase/ui/src/main.ts LiteBase/ui/src/App.vue LiteBase/ui/dist/litebase.js LiteBase/ui/dist/litebase.css
git commit -m "feat: fetch Firebase config dynamically from /litebase/config on startup"
```

---

### Task 3: Rebuild .NET to embed updated UI assets

**Step 1: Rebuild the LiteBase project**

```bash
dotnet build LiteBase/LiteBase.csproj
```

Expected: `Build succeeded`. The updated `litebase.js` and `litebase.css` from `LiteBase/ui/dist/` are now embedded in the assembly.

**Step 2: Run the demo app and verify end-to-end**

```bash
dotnet run --project DemoApp
```

Open `http://localhost:<port>/litebase/ui` in a browser.

Manual checks:
- [ ] `GET /litebase/config` returns a JSON object with Firebase fields
- [ ] The UI loads and shows the Google sign-in screen (Firebase initialised from server config)
- [ ] Removing `Firebase` from `LiteBaseOptions` in `Program.cs`, rebuilding and running shows the error screen instead of a blank/broken page

**Step 3: Commit**

```bash
git add LiteBase/obj LiteBase/bin
git commit -m "chore: rebuild to embed updated UI assets with dynamic Firebase config"
```
