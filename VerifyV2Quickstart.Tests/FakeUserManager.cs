using System;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

using VerifyV2Quickstart.Models;

namespace VerifyV2Quickstart.Tests
{
    public class FakeUserManager : UserManager<ApplicationUser>
    {
        public FakeUserManager()
            : base(Substitute.For<IUserStore<ApplicationUser>>(),
                Substitute.For<IOptions<IdentityOptions>>(),
                Substitute.For<IPasswordHasher<ApplicationUser>>(),
                new IUserValidator<ApplicationUser>[0],
                new IPasswordValidator<ApplicationUser>[0],
                Substitute.For<ILookupNormalizer>(),
                Substitute.For<IdentityErrorDescriber>(),
                Substitute.For<IServiceProvider>(),
                Substitute.For<ILogger<UserManager<ApplicationUser>>>())
        { }

    }
}