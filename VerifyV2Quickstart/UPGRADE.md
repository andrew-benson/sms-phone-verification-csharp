# Upgrade to .NET 10

This document describes the upgrade of the VerifyV2Quickstart project from .NET 8.0 to .NET 10, including the modernization of the application bootstrap code.

## Overview

- **Previous Version**: .NET 8.0 (incorrectly specified as `netcoreapp8.0`)
- **New Version**: .NET 10.0
- **Bootstrap Pattern**: Migrated from legacy `WebHost.CreateDefaultBuilder` + `Startup.cs` to modern `WebApplication.CreateBuilder()` with top-level statements
- **Architecture**: Preserved MVC + Razor Pages structure

## Files Modified

### 1. VerifyV2Quickstart.csproj

**Changes:**
- Updated `<TargetFramework>` from `netcoreapp8.0` to `net10.0`
- Upgraded all Microsoft package references from 8.0.x to 10.0.0:
  - `Microsoft.AspNetCore.Identity.UI`: 8.0.8 → 10.0.0
  - `Microsoft.AspNetCore.Identity.EntityFrameworkCore`: 8.0.8 → 10.0.0
  - `Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore`: 8.0.8 → 10.0.0
  - `Microsoft.EntityFrameworkCore.Sqlite`: 8.0.8 → 10.0.0
  - `Microsoft.EntityFrameworkCore.Tools`: 8.0.8 → 10.0.0
  - `Microsoft.VisualStudio.Web.CodeGeneration.Design`: 8.0.4 → 10.0.0
- Kept `Twilio`: 7.2.3 (unchanged)

### 2. Program.cs

**Complete rewrite** - Migrated from legacy `WebHost.CreateDefaultBuilder` pattern to modern `WebApplication.CreateBuilder()`.

**Key changes:**
- Removed namespace wrapper and class declaration (now uses top-level statements)
- Replaced `WebHost.CreateDefaultBuilder()` with `WebApplication.CreateBuilder()`
- Consolidated all service registrations from `Startup.cs` and `IdentityHostingStartup.cs`
- Added comprehensive using directives for all required namespaces
- Made `twilio.json` optional (changed from `optional: false` to `optional: true`)

**Service Registration Changes:**
- **Fixed duplicate registrations**: Removed duplicate `IVerification` registration (was registered as both scoped and singleton)
- **Fixed duplicate AddMvc calls**: Consolidated MVC configuration into `AddControllersWithViews()` and `AddRazorPages()`
- **Removed deprecated APIs**: Removed `SetCompatibilityVersion()` calls (not needed in .NET 6+)
- **Singleton for Twilio**: `IVerification` now registered as singleton only for shared Twilio client

**Middleware Pipeline Changes:**
- **Fixed middleware ordering**: Moved `UseSession()` before `UseRouting()` (correct order for .NET 6+)
- **Replaced deprecated API**: Changed `UseDatabaseErrorPage()` to `UseMigrationsEndPoint()`
- **Modern endpoint routing**: Using `app.MapRazorPages()` and `app.MapControllerRoute()` instead of `UseMvc()`

**New middleware order:**
1. `UseDeveloperExceptionPage()` / `UseMigrationsEndPoint()` (development)
2. `UseExceptionHandler()` / `UseHsts()` (production)
3. `UseStaticFiles()`
4. `UseCookiePolicy()`
5. `UseRouting()`
6. `UseSession()` ⬅️ **Moved before authentication**
7. `UseAuthentication()`
8. `UseAuthorization()`
9. `MapRazorPages()` / `MapControllerRoute()`

**Added Documentation:**
- Added XML comments warning that password requirements are weakened for development/demo purposes only
- Recommended production settings included in comments

### 3. Areas/Identity/IdentityHostingStartup.cs

**Changes:**
- Commented out `[assembly: HostingStartup(...)]` attribute to disable auto-loading
- Added note that configuration has been consolidated into `Program.cs`
- Commented out the entire `Configure()` method implementation for reference
- File kept for historical reference but is no longer active

## Files Deleted

### 1. Startup.cs

**Reason for deletion:** All configuration migrated to `Program.cs` using modern .NET 10 patterns.

**What was migrated:**
- `ConfigureServices()` method → Service registrations in `Program.cs`
- `Configure()` method → Middleware pipeline in `Program.cs`
- Cookie policy configuration
- Identity options (password requirements)
- DbContext registration
- Session configuration
- All custom service registrations

## Breaking Changes & Important Notes

### 1. Password Requirements - DEVELOPMENT ONLY

⚠️ **WARNING**: The project has intentionally weakened password requirements for demo purposes:

```csharp
options.Password.RequireDigit = false;
options.Password.RequiredLength = 1;
options.Password.RequireLowercase = false;
options.Password.RequireNonAlphanumeric = false;
options.Password.RequireUppercase = false;
```

**For production environments, restore strong password requirements:**
```csharp
options.Password.RequireDigit = true;
options.Password.RequiredLength = 8;
options.Password.RequireLowercase = true;
options.Password.RequireUppercase = true;
options.Password.RequireNonAlphanumeric = true;
```

### 2. Twilio Configuration - Optional for Local Development

The `twilio.json` file is now **optional** for local development:

```csharp
builder.Configuration.AddJsonFile("twilio.json", optional: true, reloadOnChange: false);
```

**Configuration options:**
- Use `twilio.json` file (git-ignored)
- Use User Secrets (recommended for development)
- Use environment variables (recommended for production)

### 3. Service Lifetime - Shared Twilio Client

`IVerification` is now registered as a **singleton** for shared Twilio client:

```csharp
builder.Services.AddSingleton<IVerification>(sp =>
    new Verification(builder.Configuration.GetSection("Twilio").Get<VerifyV2Quickstart.Configuration.Twilio>()));
```

This provides better performance by reusing the same Twilio client instance across all requests.

### 4. Middleware Ordering

Session middleware must come **before** authentication in .NET 6+. The project has been updated to reflect this requirement.

## Build & Run

### Prerequisites
- .NET 10 SDK installed

### Build
```bash
dotnet restore
dotnet build
```

### Run
```bash
dotnet run
```

## Testing Checklist

After upgrade, verify the following functionality:

- [ ] Application builds without errors
- [ ] Application starts successfully
- [ ] Database migrations run correctly
- [ ] User registration works
- [ ] User login works
- [ ] Phone verification flow works (if Twilio configured)
- [ ] Session management works
- [ ] Static files are served correctly
- [ ] Identity pages render correctly
- [ ] Authorization filters work as expected

## Known Issues

### Build Warnings

One minor warning remains:
```
warning CS0105: The using directive for 'Microsoft.AspNetCore.Identity' appeared previously in this namespace
```

**Location**: `/Areas/Identity/Pages/_ViewImports.cshtml`

**Impact**: None - this is a duplicate using directive in a Razor view that doesn't affect functionality.

**Resolution**: Can be safely ignored or fixed by removing the duplicate directive.

## Migration Summary

| Aspect | Before | After |
|--------|--------|-------|
| Target Framework | `netcoreapp8.0` | `net10.0` |
| Bootstrap Pattern | `WebHost.CreateDefaultBuilder` | `WebApplication.CreateBuilder` |
| Configuration Files | `Startup.cs` + `IdentityHostingStartup.cs` | Consolidated in `Program.cs` |
| Middleware API | `UseMvc()`, `UseDatabaseErrorPage()` | `MapControllerRoute()`, `UseMigrationsEndPoint()` |
| Service Registration | Duplicates present | Deduplicated and optimized |
| IVerification Lifetime | Scoped + Singleton (bug) | Singleton only |
| twilio.json | Required | Optional |

## References

- [Migrate from ASP.NET Core 8.0 to 10.0](https://learn.microsoft.com/en-us/aspnet/core/migration/80-to-90)
- [ASP.NET Core Fundamentals](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/)
- [Minimal APIs overview](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis)

---

**Upgrade Date**: 13 January 2026
**Upgrade By**: GitHub Copilot (Claude Sonnet 4.5)
