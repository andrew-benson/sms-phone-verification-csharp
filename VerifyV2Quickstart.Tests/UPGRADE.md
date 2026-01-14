# VerifyV2Quickstart.Tests Upgrade Guide

## Upgrade Date: January 14, 2026

This document describes the upgrades made to the test project to modernize the codebase.

## .NET Framework Upgrade

### Target Framework: .NET Core 3.1 → .NET 10.0

**Changed in:** `VerifyV2Quickstart.Tests.csproj`

```xml
<!-- Before -->
<TargetFramework>netcoreapp3.1</TargetFramework>

<!-- After -->
<TargetFramework>net10.0</TargetFramework>
```

**Reason:** Updated to match the main project's target framework (.NET 10) and leverage the latest .NET features, performance improvements, and security updates.

**Compatibility:** The fake Identity manager classes (`FakeUserManager` and `FakeSignInManager`) remain fully compatible with .NET 10's ASP.NET Core Identity APIs with no code changes required.

---

## NuGet Package Updates

All test-related packages were updated to their latest stable versions compatible with .NET 10:

| Package | Old Version | New Version |
|---------|-------------|-------------|
| Microsoft.NET.Test.Sdk | 16.5.0 | 17.12.0 |
| xunit | 2.4.1 | 2.9.2 |
| xunit.runner.visualstudio | 2.4.1 | 2.8.2 |

---

## Mocking Framework Migration: Moq → NSubstitute

### Replaced Package

**Removed:** `Moq` 4.13.1 → 4.20.72  
**Added:** `NSubstitute` 5.3.0

### Migration Rationale

- **Cleaner syntax:** NSubstitute eliminates `.Object` noise throughout tests
- **More intuitive API:** Direct method calls instead of `.Setup()` chains
- **Better readability:** More natural fluent interface
- **Simplified interface casting:** Built-in multi-interface substitute support

### Code Changes Required

#### Files Modified
1. `FakeUserManager.cs`
2. `FakeSignInManager.cs`
3. `PageModels/RegisterTests.cs`
4. `PageModels/VerifyTests.cs`

#### Pattern Transformations

| Moq Pattern | NSubstitute Pattern |
|-------------|---------------------|
| `using Moq;` | `using NSubstitute;` |
| `Mock<IService>` | `IService` |
| `new Mock<IService>()` | `Substitute.For<IService>()` |
| `mock.Object` | `mock` (no `.Object` needed) |
| `.Setup(x => x.Method(...))` | `x.Method(...)` |
| `.ReturnsAsync(value)` | `.Returns(value)` |
| `It.IsAny<T>()` | `Arg.Any<T>()` |
| `.Verify(x => x.Method(...), Times.Once)` | `.Received(1).Method(...)` |
| `.As<IInterface>().Setup(...)` | Multi-interface substitute |

#### Multi-Interface Substitutes

For types needing multiple interface implementations (like `IUserStore<T>` + `IUserPasswordStore<T>`):

```csharp
// Before (Moq)
_userStore = new Mock<IUserStore<ApplicationUser>>();
_userStore.As<IUserPasswordStore<ApplicationUser>>().Setup(...)

// After (NSubstitute)
_userStore = Substitute.For<IUserStore<ApplicationUser>, IUserPasswordStore<ApplicationUser>>();
((IUserPasswordStore<ApplicationUser>)_userStore).Method(...).Returns(...)
```

### Example: Before and After

#### Before (Moq)
```csharp
private readonly Mock<IVerification> _verificationService;

public RegisterTests()
{
    _verificationService = new Mock<IVerification>();
}

[Fact]
public async Task TestMethod()
{
    _verificationService.Setup(
        x => x.StartVerificationAsync(It.IsAny<string>(), It.IsAny<string>())
    ).ReturnsAsync(new VerificationResult("SID"));

    var model = new RegisterModel(GetUserManager(), _signInManage.Object, 
                                  _verificationService.Object, _logger.Object);

    var result = await model.OnPostAsync("");

    _verificationService.Verify(x => x.StartVerificationAsync("+1234567890", "sms"), Times.Once);
}
```

#### After (NSubstitute)
```csharp
private readonly IVerification _verificationService;

public RegisterTests()
{
    _verificationService = Substitute.For<IVerification>();
}

[Fact]
public async Task TestMethod()
{
    _verificationService.StartVerificationAsync(Arg.Any<string>(), Arg.Any<string>())
        .Returns(new VerificationResult("SID"));

    var model = new RegisterModel(GetUserManager(), _signInManage, 
                                  _verificationService, _logger);

    var result = await model.OnPostAsync("");

    _verificationService.Received(1).StartVerificationAsync("+1234567890", "sms");
}
```

---

## Test Results

✅ **All 10 tests passing**

- No test behavior changes required
- All existing test assertions remain valid
- Tests execute successfully on .NET 10 with NSubstitute

---

## Build Warnings

The following pre-existing warnings remain (unrelated to the upgrade):

```
warning CS4014: Because this call is not awaited, execution of the current 
method continues before the call is completed. Consider applying the 'await' 
operator to the result of the call.
```

These warnings exist in test setup code where fire-and-forget behavior is intentional and do not affect test functionality.

---

## Summary

- ✅ Upgraded from .NET Core 3.1 to .NET 10.0
- ✅ Updated all test framework packages to latest stable versions
- ✅ Migrated from Moq to NSubstitute for cleaner, more maintainable tests
- ✅ All tests passing with no behavioral changes
- ✅ Fake Identity manager classes remain compatible with .NET 10
