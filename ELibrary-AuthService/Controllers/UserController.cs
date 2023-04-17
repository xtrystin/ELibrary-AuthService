using ELibrary_AuthService.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ELibrary_AuthService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<UserController> _logger;

        public UserController(ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            ILogger<UserController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public record UserRegistrationModel(
            string FirstName,
            string LastName,
            string EmailAddress,
            string Password);

        public record UserCreatedModel(
            Guid Id,
            string FirstName,
            string LastName,
            string EmailAddress);

        [HttpPost]
        [Route("Register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register(UserRegistrationModel user)
        {
            if (ModelState.IsValid)
            {
                var existingUser = await _userManager.FindByEmailAsync(user.EmailAddress);
                if (existingUser is null)
                {
                    IdentityUser newUser = new()
                    {
                        Email = user.EmailAddress,
                        EmailConfirmed = true,       // email confirmation is not implemented
                        UserName = user.EmailAddress
                    };

                    IdentityResult result = await _userManager.CreateAsync(newUser, user.Password);
                    if (result.Succeeded)
                    {
                        existingUser = await _userManager.FindByEmailAsync(user.EmailAddress);

                        if (existingUser is null)
                        {
                            return BadRequest();
                        }

                        UserCreatedModel userDto = new UserCreatedModel(new Guid(existingUser.Id), 
                            user.FirstName,user.LastName, user.EmailAddress);
                        //send userDto to the bus

                        return Ok();
                    }
                }
            }

            return BadRequest();
        }

        public record UserChangePasswordModel(
            string userId,
            string currentPassword,
            string newPassword);

        [HttpPost]
        [Route("ChangePassword")]
        public async Task<IActionResult> ChangePassword(UserChangePasswordModel credentials)    //todo: add username from jwt?
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByIdAsync(credentials.userId);
                if (user is null)
                {
                    return NotFound();
                }

                var result = await _userManager.ChangePasswordAsync(user, credentials.currentPassword, credentials.newPassword);
                if (result.Succeeded)
                {
                    return Ok();
                }
                else
                {
                    return BadRequest(result.Errors.FirstOrDefault()?.Description); //todo: better error handling
                }
            }

            return BadRequest();
        }

        [HttpGet]
        [Route("{userId}/IsInRole")]
        [AllowAnonymous]
        public async Task<IActionResult> IsInRole([FromRoute] string userId, [FromQuery] string roleName)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByIdAsync(userId);
                bool isInRole = false;
                if (user is null)
                {
                    return NotFound();
                }
                try
                {
                    isInRole = await _userManager.IsInRoleAsync(user, roleName);

                }
                catch (Exception ex)
                {

                    throw;
                }
                return new ObjectResult(isInRole);
            }

            return BadRequest();
        }
    }
}