using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using VerifyV2Quickstart.Models;

namespace VerifyV2Quickstart.Tests
{
    public class FakeSignInManager : SignInManager<ApplicationUser>
    {
        public FakeSignInManager() : base(
            Substitute.For<FakeUserManager>(),
            new HttpContextAccessor(),
            Substitute.For<IUserClaimsPrincipalFactory<ApplicationUser>>(),
            Substitute.For<IOptions<IdentityOptions>>(),
            Substitute.For<ILogger<SignInManager<ApplicationUser>>>(),
            null, null)
        {
        }
    }
}