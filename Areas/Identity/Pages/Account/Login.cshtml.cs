using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
#nullable disable

namespace KLENZ.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, ILogger<LoginModel> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public string ReturnUrl { get; set; }

        public class InputModel
        {
            [Required]
            [Display(Name = "Username")]
            public string UserName { get; set; }

            [Required]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            // ✅ Fix returnUrl handling to prevent infinite redirects
            ReturnUrl = string.IsNullOrEmpty(returnUrl) || returnUrl.Contains("Error")
                        ? Url.Content("~/")
                        : returnUrl;

            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl = string.IsNullOrEmpty(returnUrl) || returnUrl.Contains("Error")
                        ? Url.Content("~/Home/Index")
                        : returnUrl;

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.FindByNameAsync(Input.UserName);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return Page();
            }

            var result = await _signInManager.PasswordSignInAsync(user, Input.Password, Input.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                _logger.LogInformation("User logged in.");
                return LocalRedirect(returnUrl); // ✅ Ensures a valid redirect
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return Page();
            }
        }

        // Passwords are reset by an administrator from Setup > Users.
        // The old self-service reset set a fixed, publicly known password and has been removed.
        public IActionResult OnPostResetPassword()
        {
            ModelState.Clear();
            ModelState.AddModelError(string.Empty, "Ask an administrator to reset your password from Setup > Users.");
            return Page();
        }
    }
}


//// Licensed to the .NET Foundation under one or more agreements.
//// The .NET Foundation licenses this file to you under the MIT license.
//#nullable disable

//using System;
//using System.Collections.Generic;
//using System.ComponentModel.DataAnnotations;
//using System.Linq;
//using System.Threading.Tasks;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Authentication;
//using Microsoft.AspNetCore.Identity;
//using Microsoft.AspNetCore.Identity.UI.Services;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Mvc.RazorPages;
//using Microsoft.Extensions.Logging;

//namespace KLENZ.Areas.Identity.Pages.Account
//{
//    public class LoginModel : PageModel
//    {
//        private readonly SignInManager<IdentityUser> _signInManager;
//        private readonly ILogger<LoginModel> _logger;

//        public LoginModel(SignInManager<IdentityUser> signInManager, ILogger<LoginModel> logger)
//        {
//            _signInManager = signInManager;
//            _logger = logger;
//        }

//        /// <summary>
//        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
//        ///     directly from your code. This API may change or be removed in future releases.
//        /// </summary>
//        [BindProperty]
//        public InputModel Input { get; set; }

//        /// <summary>
//        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
//        ///     directly from your code. This API may change or be removed in future releases.
//        /// </summary>
//        public IList<AuthenticationScheme> ExternalLogins { get; set; }

//        /// <summary>
//        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
//        ///     directly from your code. This API may change or be removed in future releases.
//        /// </summary>
//        public string ReturnUrl { get; set; }

//        /// <summary>
//        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
//        ///     directly from your code. This API may change or be removed in future releases.
//        /// </summary>
//        [TempData]
//        public string ErrorMessage { get; set; }

//        /// <summary>
//        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
//        ///     directly from your code. This API may change or be removed in future releases.
//        /// </summary>
//        public class InputModel
//        {
//            /// <summary>
//            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
//            ///     directly from your code. This API may change or be removed in future releases.
//            /// </summary>
//            [Required]
//            [EmailAddress]
//            public string Email { get; set; }

//            /// <summary>
//            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
//            ///     directly from your code. This API may change or be removed in future releases.
//            /// </summary>
//            [Required]
//            [DataType(DataType.Password)]
//            public string Password { get; set; }

//            /// <summary>
//            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
//            ///     directly from your code. This API may change or be removed in future releases.
//            /// </summary>
//            [Display(Name = "Remember me?")]
//            public bool RememberMe { get; set; }
//        }

//        public async Task OnGetAsync(string returnUrl = null)
//        {
//            if (!string.IsNullOrEmpty(ErrorMessage))
//            {
//                ModelState.AddModelError(string.Empty, ErrorMessage);
//            }

//            returnUrl ??= Url.Content("~/");

//            // Clear the existing external cookie to ensure a clean login process
//            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

//            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

//            ReturnUrl = returnUrl;
//        }

//        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
//        {
//            returnUrl ??= Url.Content("~/Home/Index"); // Redirect to Home after login

//            if (!ModelState.IsValid)
//            {
//                return Page();
//            }

//            var user = await _signInManager.UserManager.FindByEmailAsync(Input.Email);
//            if (user == null)
//            {
//                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
//                return Page();
//            }

//            var result = await _signInManager.PasswordSignInAsync(user.UserName, Input.Password, Input.RememberMe, lockoutOnFailure: false);

//            if (result.Succeeded)
//            {
//                return LocalRedirect(returnUrl); // ✅ Redirect to Home page
//            }
//            else
//            {
//                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
//                return Page();
//            }
//        }


//        //public async Task<IActionResult> OnPostAsync(string returnUrl = null)
//        //{
//        //    returnUrl ??= Url.Content("~/Home/Index"); // ✅ Ensures redirection to Home page

//        //    ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

//        //    if (ModelState.IsValid)
//        //    {
//        //        var user = await _signInManager.UserManager.FindByEmailAsync(Input.Email);
//        //        if (user == null)
//        //        {
//        //            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
//        //            return Page();
//        //        }

//        //        var result = await _signInManager.PasswordSignInAsync(user.UserName, Input.Password, Input.RememberMe, lockoutOnFailure: false);
//        //        if (result.Succeeded)
//        //        {
//        //            _logger.LogInformation("User logged in.");
//        //            return LocalRedirect(returnUrl); // ✅ Redirects to Home page
//        //        }
//        //        else
//        //        {
//        //            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
//        //            return Page();
//        //        }
//        //    }

//        //    return Page();
//        //}


//    }
//}
