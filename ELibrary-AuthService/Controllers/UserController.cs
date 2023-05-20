using ELibrary_AuthService.Data;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RabbitMqMessages;
using System.Text;

namespace ELibrary_AuthService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<UserController> _logger;
        private readonly IBus _bus;

        public UserController(ApplicationDbContext context,
            UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager,
            ILogger<UserController> logger, IBus bus)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
            _bus = bus;
        }

        public record UserRegistrationModel(
            string FirstName,
            string LastName,
            string EmailAddress,
            string Password);

        [HttpPost]
        [Route("Register")]
        [ProducesResponseType(400, Type = typeof(IdentityError))]
        [ProducesResponseType(200)]
        [AllowAnonymous]
        public async Task<IActionResult> Register(UserRegistrationModel user)
        {
            if (ModelState.IsValid)
            {
                var existingUser = await _userManager.FindByEmailAsync(user.EmailAddress);
                if (existingUser is not null)
                    return BadRequest("Account with this email already exist");
                

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
                        return BadRequest();

                    var message = new UserCreated() { UserId = new Guid(existingUser.Id),
                        FirstName = user.FirstName, LastName = user.LastName };
                    
                    //await _bus.Publish(message);    // todo: uncomment when rabbit is ready
                    return Ok();
                }
                else
                {
                    StringBuilder message = new("");
                    foreach (var error in result.Errors)
                    {
                        message.Append(error.Description + "\n");
                    }

                    return BadRequest(message.ToString());
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

        [HttpPost]
        [Route("{userId}/AddToRole")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> AddToRole([FromRoute] string userId, [FromQuery] string roleName)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null) 
                return NotFound();

            try
            {
                var isInRole = await _userManager.IsInRoleAsync(user, roleName);
                if (isInRole)
                    return BadRequest("User is already in this role");

                if (await _roleManager.RoleExistsAsync(roleName) is false)
                    return BadRequest("This role does not exist");

                await _userManager.AddToRoleAsync(user, roleName);
                return Ok();
            }
            catch (Exception ex)
            {
                throw;
            }
            
        }
    }
}