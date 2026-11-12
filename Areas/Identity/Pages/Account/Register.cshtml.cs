// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace KLENZ.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserStore<ApplicationUser> _userStore;
        private readonly IUserEmailStore<ApplicationUser> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            IUserStore<ApplicationUser> userStore,
            SignInManager<ApplicationUser> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string ReturnUrl { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            [Required]
            [Display(Name = "Username")]
            public string UserName { get; set; }

            [EmailAddress]
            [Display(Name = "Email (Optional)")]
            public string Email { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }
        }

        /// <summary>True when the signed-in user is creating an account for someone else.</summary>
        public bool CreatingForSomeoneElse { get; set; }

        // New accounts can be created only when no account exists yet (first-time setup)
        // or by a signed-in administrator. Until an administrator has been assigned,
        // any signed-in user may create accounts so the team is never locked out.
        private async Task<bool> CanRegisterAsync()
        {
            if (!_userManager.Users.Any())
            {
                return true;
            }

            if (User.Identity?.IsAuthenticated != true)
            {
                return false;
            }

            var me = await _userManager.GetUserAsync(User);
            if (me != null && me.IsAdmin == 1)
            {
                return true;
            }

            return !_userManager.Users.Any(u => u.IsAdmin == 1);
        }

        public async Task<IActionResult> OnGetAsync(string returnUrl = null)
        {
            if (!await CanRegisterAsync())
            {
                TempData["ErrorMessage"] = "Only an administrator can create new accounts.";
                return RedirectToPage("./Login");
            }

            ReturnUrl = returnUrl;
            CreatingForSomeoneElse = User.Identity?.IsAuthenticated == true;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= "/Home/Index"; // Ensure proper local path

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (!await CanRegisterAsync())
            {
                TempData["ErrorMessage"] = "Only an administrator can create new accounts.";
                return RedirectToPage("./Login");
            }

            CreatingForSomeoneElse = User.Identity?.IsAuthenticated == true;
            var isFirstAccount = !_userManager.Users.Any();

            if (ModelState.IsValid)
            {
                var user = CreateUser();
                if (isFirstAccount)
                {
                    user.IsAdmin = 1; // the first account administers the workspace
                }
                await _userStore.SetUserNameAsync(user, Input.UserName, CancellationToken.None);

                if (!string.IsNullOrEmpty(Input.Email))
                {
                    await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);
                }

                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User created a new account with password.");

                    if (CreatingForSomeoneElse)
                    {
                        TempData["SuccessMessage"] = "Account created for " + Input.UserName + ".";
                        return Redirect("/Users");
                    }

                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return Redirect("/Home/Index");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return Page();
        }


        private ApplicationUser CreateUser()
        {
            try
            {
                return new ApplicationUser();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(IdentityUser)}'. Ensure it has a parameterless constructor.");
            }
        }


        private IUserEmailStore<ApplicationUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }
            return (IUserEmailStore<ApplicationUser>)_userStore;
        }
    }
}
