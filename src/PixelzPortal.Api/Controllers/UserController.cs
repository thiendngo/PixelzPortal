using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PixelzPortal.Domain.Entities;

namespace PixelzPortal.Api.Controllers
{
    [ApiController]
    [Route("api/users")]
    [Authorize(Roles = "ItSupport,Manager")]
    public class UserController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;

        public UserController(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }

        /// <summary>
        /// Get all registered users (accessible to ITSupport and Manager only)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = _userManager.Users
                .Select(u => new
                {
                    u.Id,
                    u.UserName,
                    u.Email,
                    u.EmailConfirmed
                })
                .ToList();

            return Ok(users);
        }
    }
}
