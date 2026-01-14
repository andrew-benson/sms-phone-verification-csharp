using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using VerifyV2Quickstart.Areas.Identity.Pages.Account;
using VerifyV2Quickstart.Models;
using VerifyV2Quickstart.Services;
using Xunit;

namespace VerifyV2Quickstart.Tests.PageModels
{
    public class RegisterTests
    {
        private readonly IVerification _verificationService;
        private readonly FakeSignInManager _signInManage;
        private readonly IUserStore<ApplicationUser> _userStore;
        private readonly ILogger<RegisterModel> _logger;

        public RegisterTests()
        {
            _userStore = Substitute.For<IUserStore<ApplicationUser>, IUserPasswordStore<ApplicationUser>>();
            _verificationService = Substitute.For<IVerification>();
            _logger = Substitute.For<ILogger<RegisterModel>>();
            _signInManage = Substitute.For<FakeSignInManager>();
        }

        private UserManager<ApplicationUser> GetUserManager()
        {
            var hasher = Substitute.For<IPasswordHasher<ApplicationUser>>();

            return new UserManager<ApplicationUser>(_userStore, null, hasher, null,
                null, null, null, null, null);
        }


        [Fact]
        public void OnGetReturnUrlIsAssigned()
        {
            // Arrange
            var registerModel = new RegisterModel(GetUserManager(), _signInManage, _verificationService, _logger);

            // Act
            registerModel.OnGet("returnUrl");

            // Assert
            Assert.Equal("returnUrl", registerModel.ReturnUrl);
        }

        [Fact]
        public async Task OnPostWithInvalidModelStateThenReturnsPage()
        {
            var registerModel = new RegisterModel(GetUserManager(), _signInManage, _verificationService, _logger);

            // Arrange
            registerModel.ModelState.AddModelError("key", "Another error");

            // Act
            var result = await registerModel.OnPostAsync("");

            // Assert
            Assert.IsType<PageResult>(result);
        }

        [Fact]
        public async Task OnPostWithValidModelStateAndUserCreationFailsThenReturnsPage()
        {
            // Arrange
            ((IUserPasswordStore<ApplicationUser>)_userStore).CreateAsync(
                Arg.Any<ApplicationUser>(), Arg.Any<CancellationToken>()
            ).Returns(
                IdentityResult.Failed(new IdentityError {Code = "1", Description = "Error"})
            );

            var registerModel = new RegisterModel(GetUserManager(), _signInManage, _verificationService, _logger);

            registerModel.Input = new RegisterModel.InputModel
            {
                UserName = "username", Password = "Pas$w0rd", FullPhoneNumber = "+1234567890"
            };

            // Act
            var result = await registerModel.OnPostAsync("");

            // Assert
            Assert.False(registerModel.ModelState.IsValid);
            Assert.IsType<PageResult>(result);
        }

        [Fact]
        public async Task OnPostWithValidModelAndUserCreatedAndVerificationStartFailsThenInvalidateModel()
        {
            // Arrange
            ((IUserPasswordStore<ApplicationUser>)_userStore).CreateAsync(
                Arg.Any<ApplicationUser>(), Arg.Any<CancellationToken>()
            ).Returns(IdentityResult.Success);

            _verificationService.StartVerificationAsync(Arg.Any<string>(), Arg.Any<string>())
                .Returns(new VerificationResult(new List<string> {"Error"}));

            var registerModel = new RegisterModel(GetUserManager(), _signInManage, _verificationService, _logger);

            registerModel.Input = new RegisterModel.InputModel
            {
                UserName = "username", Password = "Pas$w0rd", FullPhoneNumber = "+1234567890", Channel = "sms"
            };

            // Act
            var result = await registerModel.OnPostAsync("");

            // Assert
            Assert.False(registerModel.ModelState.IsValid);
            Assert.IsType<PageResult>(result);
            _verificationService.Received(1).StartVerificationAsync("+1234567890", "sms");
        }

        [Fact]
        public async Task OnPostWithValidModelAndUserCreatedAndVerificationStartedThenRedirectToVerify()
        {
            // Arrange
            ((IUserPasswordStore<ApplicationUser>)_userStore).CreateAsync(
                Arg.Any<ApplicationUser>(), Arg.Any<CancellationToken>()
            ).Returns(IdentityResult.Success);

            _verificationService.StartVerificationAsync(Arg.Any<string>(), Arg.Any<string>())
                .Returns(new VerificationResult("SID"));

            var registerModel = new RegisterModel(GetUserManager(), _signInManage, _verificationService, _logger);

            registerModel.Input = new RegisterModel.InputModel
            {
                UserName = "username", Password = "Pas$w0rd", FullPhoneNumber = "+1234567890", Channel = "sms"
            };

            var context = Substitute.For<HttpContext>();
            context.Session.Returns(Substitute.For<ISession>());
            registerModel.PageContext.HttpContext = context;

            var urlHelper = Substitute.For<IUrlHelper>();
            urlHelper.Content(Arg.Any<string>()).Returns("redirect");
            registerModel.Url = urlHelper;

            // Act
            var result = await registerModel.OnPostAsync("return");

            // Assert
            Assert.True(registerModel.ModelState.IsValid);
            Assert.IsType<LocalRedirectResult>(result);
            Assert.Equal("redirect", (result as LocalRedirectResult)?.Url);
            urlHelper.Received(1).Content("~/Identity/Account/Verify/?returnUrl=return");
            _verificationService.Received(1).StartVerificationAsync("+1234567890", "sms");
        }
    }
}