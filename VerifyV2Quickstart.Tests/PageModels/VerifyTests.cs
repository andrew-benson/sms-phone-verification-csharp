using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using NSubstitute;
using VerifyV2Quickstart.Areas.Identity.Pages.Account;
using VerifyV2Quickstart.Models;
using VerifyV2Quickstart.Services;
using Xunit;

namespace VerifyV2Quickstart.Tests.PageModels
{
    public class VerifyTests
    {
        private readonly IVerification _verificationService;
        private readonly IUserStore<ApplicationUser> _userStore;
        private readonly ILogger<VerifyModel> _logger;

        public VerifyTests()
        {
            _userStore = Substitute.For<IUserStore<ApplicationUser>>();
            _verificationService = Substitute.For<IVerification>();
            _logger = Substitute.For<ILogger<VerifyModel>>();
        }

        private UserManager<ApplicationUser> GetUserManager()
        {
            var hasher = Substitute.For<IPasswordHasher<ApplicationUser>>();

            return new UserManager<ApplicationUser>(_userStore, null, hasher, null,
                null, null, null, null, null);
        }

        [Fact]
        public void OnGetReturnUrlIsAssignedAndPageReturned()
        {
            // Arrange
            var verifyModel = new VerifyModel(GetUserManager(), _verificationService, _logger);
            var context = Substitute.For<HttpContext>();
            context.User.Returns(Substitute.For<ClaimsPrincipal>());
            verifyModel.PageContext.HttpContext = context;

            // Act
            var result = verifyModel.OnGet("returnUrl");

            // Assert
            Assert.Equal("returnUrl", verifyModel.ReturnUrl);
            Assert.IsType<PageResult>(result);
        }

        [Fact]
        public async Task OnPosWithoutLoggedInUserThenRedirectToLogin()
        {
            // Arrange
            _userStore.FindByIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(new ApplicationUser());

            var verifyModel = new VerifyModel(GetUserManager(), _verificationService, _logger);
            var context = Substitute.For<HttpContext>();
            context.User.Returns(Substitute.For<ClaimsPrincipal>());
            verifyModel.PageContext.HttpContext = context;

            var urlHelper = Substitute.For<IUrlHelper>();
            urlHelper.Content(Arg.Any<string>()).Returns("redirect");
            verifyModel.Url = urlHelper;

            // Act
            var result = await verifyModel.OnPostAsync("");

            // Assert
            Assert.IsType<LocalRedirectResult>(result);
        }

        [Fact]
        public async Task OnPostWithLoggedInUserAndInvalidModelStateThenReturnPage()
        {
            // Arrange
            _userStore.FindByIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(new ApplicationUser());

            var verifyModel = new VerifyModel(GetUserManager(), _verificationService, _logger);
            var context = Substitute.For<HttpContext>();
            var session = Substitute.For<ISession>();
            var principal = Substitute.For<ClaimsPrincipal>();
            principal.FindFirst(Arg.Any<string>())
                .Returns(new Claim("name", "John Doe"));
            byte[] value;
            session.TryGetValue(Arg.Any<string>(), out value).Returns(false);
            context.Session.Returns(session);
            context.User.Returns(principal);
            verifyModel.PageContext.HttpContext = context;

            verifyModel.ModelState.AddModelError("key", "Another error");

            // Act
            var result = await verifyModel.OnPostAsync("");

            // Assert
            Assert.IsType<PageResult>(result);
        }

        [Fact]
        public async Task OnPostWithLoggedInUserAndCodeVerificationFailThenModeStateInvalid()
        {
            // Arrange
            _userStore.FindByIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(new ApplicationUser {PhoneNumber = "+1234567890"});

            var verifyModel = new VerifyModel(GetUserManager(), _verificationService, _logger);
            var context = Substitute.For<HttpContext>();
            var session = Substitute.For<ISession>();
            var principal = Substitute.For<ClaimsPrincipal>();
            principal.FindFirst(Arg.Any<string>())
                .Returns(new Claim("name", "John Doe"));
            byte[] value;
            session.TryGetValue(Arg.Any<string>(), out value).Returns(false);
            context.Session.Returns(session);
            context.User.Returns(principal);

            _verificationService.CheckVerificationAsync(Arg.Any<string>(), Arg.Any<string>())
                .Returns(new VerificationResult(new List<string> {"Failed"}));

            verifyModel.Input = new VerifyModel.InputModel
            {
                Code = "123456"
            };

            verifyModel.PageContext.HttpContext = context;

            // Act
            var result = await verifyModel.OnPostAsync("");

            // Assert
            Assert.IsType<PageResult>(result);
            Assert.False(verifyModel.ModelState.IsValid);
            _verificationService.Received(1).CheckVerificationAsync("+1234567890", "123456");
        }

        [Fact]
        public async Task OnPostWithLoggedInUserAndCodeVerificationFailThenUserUpdateAndRedirectsHome()
        {
            // Arrange
            _userStore.FindByIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(new ApplicationUser {PhoneNumber = "+1234567890"});

            var verifyModel = new VerifyModel(GetUserManager(), _verificationService, _logger);
            var context = Substitute.For<HttpContext>();
            var session = Substitute.For<ISession>();
            var principal = Substitute.For<ClaimsPrincipal>();
            principal.FindFirst(Arg.Any<string>())
                .Returns(new Claim("name", "John Doe"));
            byte[] value;
            session.TryGetValue(Arg.Any<string>(), out value).Returns(false);
            context.Session.Returns(session);
            context.User.Returns(principal);

            var urlHelper = Substitute.For<IUrlHelper>();
            urlHelper.Content(Arg.Any<string>()).Returns("redirect");
            verifyModel.Url = urlHelper;

            _verificationService.CheckVerificationAsync(Arg.Any<string>(), Arg.Any<string>())
                .Returns(new VerificationResult("SID"));

            verifyModel.Input = new VerifyModel.InputModel
            {
                Code = "123456"
            };

            verifyModel.PageContext.HttpContext = context;

            // Act
            var result = await verifyModel.OnPostAsync("");

            // Assert
            Assert.IsType<LocalRedirectResult>(result);
            Assert.True(verifyModel.ModelState.IsValid);
            _verificationService.Received(1).CheckVerificationAsync("+1234567890", "123456");
        }
    }
}