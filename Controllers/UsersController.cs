using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KLENZ.Controllers
{
    /// <summary>
    /// Setup > Users. Administrators create accounts, grant or remove administrator
    /// access, and reset passwords. Until an administrator has been assigned, any
    /// signed-in user can use this page so the team is never locked out.
    /// </summary>
    [Authorize]
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public UsersController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        private async Task<bool> CanManageUsersAsync()
        {
            var me = await _userManager.GetUserAsync(User);
            if (me == null)
            {
                return false;
            }

            if (me.IsAdmin == 1)
            {
                return true;
            }

            // No administrator yet: allow setup by any signed-in user.
            return !await _userManager.Users.AnyAsync(u => u.IsAdmin == 1);
        }

        // GET: Users
        public async Task<IActionResult> Index()
        {
            ViewBag.CanManage = await CanManageUsersAsync();
            ViewBag.CurrentUserId = _userManager.GetUserId(User);

            var users = await _userManager.Users
                .OrderBy(u => u.UserName)
                .ToListAsync();

            return View(users);
        }

        // POST: Users/ToggleAdmin/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAdmin(string id)
        {
            if (!await CanManageUsersAsync())
            {
                return Forbid();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var makeAdmin = user.IsAdmin != 1;
            if (!makeAdmin)
            {
                var adminCount = await _userManager.Users.CountAsync(u => u.IsAdmin == 1);
                if (adminCount <= 1)
                {
                    TempData["ErrorMessage"] = "Keep at least one administrator.";
                    return RedirectToAction(nameof(Index));
                }
            }

            user.IsAdmin = makeAdmin ? (byte)1 : (byte)0;
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = makeAdmin
                    ? user.UserName + " is now an administrator."
                    : user.UserName + " is no longer an administrator.";
            }
            else
            {
                TempData["ErrorMessage"] = string.Join(" ", result.Errors.Select(e => e.Description));
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Users/ResetPassword/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string id)
        {
            if (!await CanManageUsersAsync())
            {
                return Forbid();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var temporaryPassword = NewTemporaryPassword();
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, temporaryPassword);

            if (result.Succeeded)
            {
                // Shown once to the administrator, who passes it on to the user.
                TempData["TemporaryPasswordUser"] = user.UserName;
                TempData["TemporaryPassword"] = temporaryPassword;
            }
            else
            {
                TempData["ErrorMessage"] = string.Join(" ", result.Errors.Select(e => e.Description));
            }

            return RedirectToAction(nameof(Index));
        }

        private static string NewTemporaryPassword()
        {
            const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string lower = "abcdefghijkmnpqrstuvwxyz";
            const string digits = "23456789";
            const string symbols = "@#$%";
            const string any = upper + lower + digits;

            var chars = new List<char>
            {
                Pick(upper), Pick(lower), Pick(digits), Pick(symbols)
            };

            for (var i = 0; i < 8; i++)
            {
                chars.Add(Pick(any));
            }

            for (var i = chars.Count - 1; i > 0; i--)
            {
                var j = RandomNumberGenerator.GetInt32(i + 1);
                (chars[i], chars[j]) = (chars[j], chars[i]);
            }

            return new string(chars.ToArray());
        }

        private static char Pick(string source)
        {
            return source[RandomNumberGenerator.GetInt32(source.Length)];
        }
    }
}
