# ShopLite — .NET MAUI e-commerce MVP

A small, working e-commerce app: a **.NET MAUI** mobile client (Android and iOS) and an **ASP.NET Core Web API** with **PostgreSQL**.

Users can register, sign in, browse and search products, filter by category, open product details, manage a cart, place a **cash-on-delivery** order, see their order history, and log out.

```
Ecommerce/
├── Backend/                    ASP.NET Core Web API (.NET 10, EF Core 10, PostgreSQL)
│   ├── Controllers/            Auth, Catalog (products + categories), Cart, Orders
│   ├── Data/                   AppDbContext, SeedData, Migrations/
│   ├── DTOs/                   Request/response contracts with validation attributes
│   ├── Middleware/             Centralized exception → problem+json handling
│   ├── Models/                 EF Core entities
│   ├── Services/               Business logic (auth, catalog, cart, orders, JWT)
│   └── Dockerfile
├── Mobile/                     .NET MAUI app (Android + iOS), MVVM
│   ├── Components/             ProductCard, StateView (loading/empty/error), StatusBadge
│   ├── Converters/             Currency + order-status color converters
│   ├── Models/                 API models + observable UI models
│   ├── Resources/              Styles (design tokens), fonts, icons, splash
│   ├── Services/               ApiClient, Auth/Session, Product/Cart/Order services, settings
│   ├── ViewModels/             CommunityToolkit.Mvvm view models
│   ├── Views/                  XAML pages
│   ├── appsettings.json        API URLs (dev)
│   └── appsettings.Release.json
├── scripts/smoke-test.sh       End-to-end API test (curl + jq + psql)
├── docker-compose.yml          PostgreSQL (+ optional API container)
├── .env.example                Environment variable template
└── Ecommerce.slnx
```

## Tech stack

| Layer | Technology |
|---|---|
| Mobile | .NET 10 MAUI, C#, XAML (compiled bindings, XAML source generator), MVVM with CommunityToolkit.Mvvm 8.4, CommunityToolkit.Maui 16 |
| API | ASP.NET Core 10 Web API, JWT bearer auth, Swagger (Swashbuckle 10) |
| Data | Entity Framework Core 10, Npgsql 10, PostgreSQL 17 |
| Security | PBKDF2 password hashing (ASP.NET Core Identity `PasswordHasher`), JWT (HS256), tokens in platform SecureStorage |

---

## 1. Required software

| Tool | Version | Notes |
|---|---|---|
| .NET SDK | **10.0.x** (tested with 10.0.404) | `dotnet --list-sdks` |
| MAUI workloads | `maui-android`, `maui-ios` | `dotnet workload install maui-android maui-ios` |
| Docker Desktop | any recent | Runs PostgreSQL (and optionally the API) |
| Android SDK | API 36 platform + an emulator image | Android Studio is the easiest way to get it |
| JDK | **17 or 21** (not newer) | .NET for Android rejects JDK 22+ |
| Xcode (macOS only) | Xcode 26.x matching the installed iOS workload | Needed for any iOS build or simulator |
| jq, curl | — | Only for `scripts/smoke-test.sh` |

## 2. Install dependencies

```bash
# .NET workloads for the mobile app
dotnet workload install maui-android maui-ios      # use "maui-android" only on Windows/Linux

# Restore NuGet packages and local tools (dotnet-ef)
cd Ecommerce
dotnet tool restore
dotnet restore Backend
dotnet restore Mobile
```

If you don't have JDK 17/21 or the Android API 36 platform, the .NET Android SDK can install them:

```bash
dotnet build Mobile -t:InstallAndroidDependencies -f net10.0-android \
  -p:AndroidSdkDirectory="$HOME/Library/Android/sdk" \
  -p:JavaSdkDirectory="$HOME/Library/Java/microsoft-jdk" \
  -p:AcceptAndroidSDKLicenses=True
```

## 3. Configure the database

```bash
cp .env.example .env          # then edit POSTGRES_PASSWORD and JWT_SECRET
docker compose up -d db       # PostgreSQL 17 on localhost:5433
```

The host port defaults to **5433** (so it doesn't clash with a local PostgreSQL on 5432). Change `POSTGRES_PORT` in `.env` if needed.

Backend configuration (all can be overridden with environment variables using `__` as separator):

| Setting | Env var | Purpose |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | `ConnectionStrings__DefaultConnection` | PostgreSQL connection string |
| `Jwt:Secret` | `Jwt__Secret` | HMAC signing key, **≥ 32 characters** (startup fails otherwise) |
| `Jwt:Issuer`, `Jwt:Audience`, `Jwt:ExpiryMinutes` | `Jwt__Issuer`, … | Token settings (default expiry: 7 days) |
| `Database:MigrateOnStartup` | `Database__MigrateOnStartup` | Apply EF migrations at startup (on in Development and Docker) |
| `Swagger:Enabled` | `Swagger__Enabled` | Serve Swagger UI outside Development |

`Backend/appsettings.Development.json` contains **development-only** values (local DB password matching `.env.example`-style setup, and a throwaway JWT secret). For anything shared, use `dotnet user-secrets` or environment variables instead:

```bash
cd Backend
dotnet user-secrets set "Jwt:Secret" "$(openssl rand -base64 48)"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5433;Database=ecommerce;Username=ecommerce;Password=<your password>"
```

> The development connection string uses password `ecommerce_dev_password`. Either set `POSTGRES_PASSWORD=ecommerce_dev_password` in your `.env`, or override the connection string as shown above.

## 4. Run database migrations

Migrations live in `Backend/Data/Migrations`. The initial migration creates all tables, constraints, indexes **and the seed data** (5 categories, 24 products).

```bash
dotnet tool restore
dotnet ef database update --project Backend
```

In Development (and in the Docker image) migrations are also applied automatically at startup.

To add a migration after changing the model:

```bash
dotnet ef migrations add <Name> --project Backend --output-dir Data/Migrations
```

## 5. Start the backend

```bash
cd Backend
dotnet run --launch-profile http          # http://localhost:5080 (listens on all interfaces)
```

- Swagger UI: <http://localhost:5080/swagger> — use **Authorize** with the token from `/api/auth/login`.
- Health check: <http://localhost:5080/health>

Or run everything in Docker:

```bash
docker compose up -d --build              # db + api on http://localhost:5080
```

### API

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| POST | `/api/auth/register` | – | Create account → `{ token, expiresAt, user }` |
| POST | `/api/auth/login` | – | Sign in → `{ token, expiresAt, user }` |
| GET | `/api/auth/me` | ✔ | Current user profile |
| GET | `/api/products?search=&categoryId=&featured=` | – | List / search / filter products |
| GET | `/api/products/{id}` | – | Product details |
| GET | `/api/categories` | – | Categories with product counts |
| GET | `/api/cart` | ✔ | Current cart with server-calculated totals |
| POST | `/api/cart/items` | ✔ | `{ productId, quantity }` – add (or increase) |
| PATCH | `/api/cart/items/{id}` | ✔ | `{ quantity }` – set quantity |
| DELETE | `/api/cart/items/{id}` | ✔ | Remove item |
| POST | `/api/orders` | ✔ | `{ customerName, phone, deliveryAddress }` – place COD order from the cart |
| GET | `/api/orders` | ✔ | Order history |
| GET | `/api/orders/{id}` | ✔ | Order details (own orders only) |

Errors are returned as RFC 7807 `application/problem+json` (validation errors include an `errors` dictionary).

Business rules worth knowing:

- **Totals are computed on the server.** The order request contains only delivery details; items come from the server-side cart and prices from the database. Any price/total sent by a client is ignored.
- Placing an order runs in a transaction: stock is decremented with a conditional `UPDATE … WHERE Stock >= qty`, so concurrent orders can never oversell (the loser gets `409 Conflict`). The cart is emptied on success.
- `OrderItem` stores snapshots of product name, image and unit price, so order history is unaffected by later catalog changes.
- New orders start as `Pending`. Statuses: `Pending`, `Confirmed`, `Shipped`, `Delivered`, `Cancelled`.

## 6. Run the mobile app

The API base URL comes from `Mobile/appsettings.json` (embedded at build time):

```json
{
  "Api": {
    "BaseUrl": "http://localhost:5080/",          // iOS simulator
    "AndroidBaseUrl": "http://10.0.2.2:5080/",    // Android emulator → host machine
    "TimeoutSeconds": 20
  }
}
```

`appsettings.{ApiEnvironment}.json` is layered on top; `ApiEnvironment` defaults to the build configuration, so **Release builds use `appsettings.Release.json`** (put your HTTPS production URL there). To point a Release build at your local API, pass `-p:ApiEnvironment=Development`.

```bash
# Android emulator (start the emulator first, see below)
dotnet build Mobile -t:Run -f net10.0-android

# iOS simulator (macOS + Xcode)
dotnet build Mobile -t:Run -f net10.0-ios
```

Or open `Ecommerce.slnx` in Visual Studio Code (C# Dev Kit + .NET MAUI extension), Visual Studio, or Rider and pick a target.

**Physical devices:** set `BaseUrl`/`AndroidBaseUrl` to your machine's LAN IP (e.g. `http://192.168.1.20:5080/`). On Android also add that IP to `Mobile/Platforms/Android/Resources/xml/network_security_config.xml` (plain HTTP is blocked everywhere else). The backend already listens on `0.0.0.0:5080`.

## 7. Configure the Android emulator

1. Install **Android Studio** → *More Actions → SDK Manager*: install *Android SDK Platform 36*, *Android SDK Build-Tools*, *Android Emulator*, *Android SDK Platform-Tools*.
2. *More Actions → Virtual Device Manager → Create device* (e.g. Pixel / Medium Phone) with an **arm64** image on Apple Silicon (x86_64 on Intel/Windows). Give it at least 2 GB RAM.
3. Start it (`emulator -avd <name>` or from Android Studio), then `adb devices` should list it.
4. If MSBuild can't find the SDK/JDK, pass `-p:AndroidSdkDirectory=...` and `-p:JavaSdkDirectory=...` (or set `ANDROID_HOME` / `JAVA_HOME`).

The emulator reaches your computer at **10.0.2.2** — that's why `AndroidBaseUrl` uses it.

## 8. Configure the iOS simulator (macOS)

1. Install **Xcode** from the App Store (the version required by the installed `maui-ios` workload — check `dotnet workload list` / the MAUI release notes).
2. `sudo xcode-select -s /Applications/Xcode.app` and run Xcode once to accept the license and install the iOS platform/simulator runtime (*Xcode → Settings → Components*).
3. `xcrun simctl list devices` should list simulators.
4. Run: `dotnet build Mobile -t:Run -f net10.0-ios` (optionally `-p:_DeviceName=:v2:udid=<UDID>` to choose a simulator).

The simulator shares the Mac's network, so `http://localhost:5080` works. `Info.plist` allows plain HTTP only for local networking (`NSAllowsLocalNetworking`); production must use HTTPS. `Entitlements.plist` enables Keychain access required by `SecureStorage` on the simulator.

## 9. Testing

Backend end-to-end test (needs the API and the `db` container running):

```bash
./scripts/smoke-test.sh                 # defaults to http://localhost:5080
```

It registers a fresh user and exercises the full flow — registration, login, catalog, search, details, cart add/update/remove, totals, order placement, PostgreSQL verification, order history, user isolation and logout — plus negative cases (validation, duplicates, out of stock, price tampering, missing/invalid tokens).

### Test results (2026-11-26, macOS 15.7 arm64, .NET SDK 10.0.404)

**Backend — `scripts/smoke-test.sh`: 40/40 passed** against the real API + PostgreSQL 17, covering all 12 required scenarios plus negative cases. A separate concurrency check (two users ordering the last units of a product simultaneously) produced one `201` and one `409`, and stock never went negative.

**Builds**

| Target | Result |
|---|---|
| Backend (`dotnet build`) | ✅ 0 warnings, 0 errors |
| Android Debug (`net10.0-android`) | ✅ 0 warnings, 0 errors |
| Android Release APK (`android-arm64`, trimmed) | ✅ 0 warnings, 0 errors |
| iOS (`net10.0-ios`) | ⚠️ Not built — Xcode is not installed on the test machine |

**Mobile app on the Android emulator (API 37, Release build)**

| # | Scenario | Result |
|---|---|---|
| 1 | Register | ✅ Account created from the app; row verified in PostgreSQL (PBKDF2 hash) |
| 2 | Log in / session | ✅ Session restored from SecureStorage on relaunch (skips login) |
| 3 | Products load | ✅ Home: categories + featured products + images from the API |
| 4 | Search | ✅ Search from Home opens Shop filtered ("headphones" → 2 products); category chip filter works |
| 5 | Product details | ✅ Image, category, price, availability, description, quantity stepper |
| 6–12 | Cart, quantities, total, checkout, order history, logout | ⏳ Verified at API level only (smoke test); UI run interrupted — see Known limitations |

## 10. Troubleshooting

| Problem | Fix |
|---|---|
| `XA0030: Building with JDK version 25 is not supported` | Use JDK 17/21: `-p:JavaSdkDirectory=...` or the `InstallAndroidDependencies` command above. |
| `XA5207: Could not find android.jar for API level 36` | Install *Android SDK Platform 36* (SDK Manager or `InstallAndroidDependencies`). |
| App shows "Can't reach the server" | Is the API running on port 5080? Android emulator must use `10.0.2.2`, iOS simulator `localhost`, devices the LAN IP. Check the URL on the Profile screen. |
| Android: `CLEARTEXT communication not permitted` | Add the host to `network_security_config.xml`, or use HTTPS. |
| API fails at startup: `Jwt:Secret must be configured` | Set `Jwt__Secret` (≥ 32 chars) via env var, user-secrets or `.env` (Docker). |
| `password authentication failed for user "ecommerce"` | The DB password in the connection string must match `POSTGRES_PASSWORD` used when the volume was **first** created. Reset with `docker compose down -v` (deletes data). |
| Port 5432/5433/5080 already in use | Change `POSTGRES_PORT` / `API_PORT` in `.env` and the connection string / `applicationUrl`. |
| iOS build: `Could not find a valid Xcode app bundle` | Install Xcode and run `sudo xcode-select -s /Applications/Xcode.app`. |
| iOS simulator: SecureStorage throws | Make sure `Platforms/iOS/Entitlements.plist` is used (`CodesignEntitlements` in the csproj). |
| Emulator is extremely slow / "isn't responding" dialogs | Give Docker Desktop and the emulator enough RAM, close other heavy apps, and prefer a Release build for UI testing: `dotnet build Mobile -t:Run -c Release -f net10.0-android -p:ApiEnvironment=Development -p:AndroidPackageFormat=apk`. |
| Scrolling is choppy in the emulator | Check the emulator log for `gles_mode_selected:swangle` (software rendering on the CPU). Start it with the host GPU instead: `emulator -avd <name> -gpu host` (or set *Graphics: Hardware* in the AVD settings). In our test this took janky frames from 44% to 2%. |
| `dotnet ef` not found | `dotnet tool restore` (the tool is pinned in `dotnet-tools.json`). |

## 11. Known limitations

- **iOS is untested.** The project targets `net10.0-ios` (Info.plist ATS exception for local networking, Keychain entitlement for SecureStorage), but it could not be compiled or run because Xcode is not installed on the development machine.
- **Mobile UI verification is partial.** Scenarios 6–12 (cart, quantities, checkout, orders, logout) are implemented and pass end-to-end at the API level, but the run on the Android emulator was interrupted (host out of memory and disk).
- **Unconfirmed navigation glitch.** Twice, under heavy emulator lag, tapping a product card in *Shop* switched to another tab instead of opening the product. It did not reproduce once the emulator was responsive (navigation log: `//main/catalog → //main/catalog/product`). No app code navigates to those tabs, so it is most likely delayed touch input, but this is unconfirmed. Debug builds log every Shell navigation as `[NAV]` lines for diagnosis.
- Order status stays `Pending`: there is no admin panel/endpoint to move orders through `Confirmed → Shipped → Delivered`.
- Only cash on delivery is supported; no payment provider.
- Logout is client-side (the JWT is discarded). Issued tokens stay valid until they expire (7 days by default); there are no refresh tokens or server-side revocation.
- Product images are hot-linked from Unsplash (free license) and require internet access; there is no image upload or admin catalog management.
- The product list is not paginated (fine for the seeded catalog, not for thousands of products).
- Prices are in USD only; UI text is English only.
- The API serves plain HTTP for local development; put it behind HTTPS (reverse proxy) in production and set the HTTPS URL in `Mobile/appsettings.Release.json`.
- No rate limiting on login/registration endpoints.
