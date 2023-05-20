using ELibrary_AuthService.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ELibrary_AuthService.Controllers;

public class TokenController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IConfiguration _config;

    public TokenController(ApplicationDbContext context,
                           UserManager<IdentityUser> userManager,
                           IConfiguration config)
    {
        _context = context;
        _userManager = userManager;
        _config = config;
    }

    public record LoginCredentials(string Username, string Password, string Grant_type);

    [Route("/token")]
    [HttpPost]
    [ProducesResponseType(typeof(string), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Create([FromBody] LoginCredentials credentials)
    {
        if (await IsValidUsernameAndPassword(credentials.Username, credentials.Password))
        {
            return new ObjectResult(await GenerateToken(credentials.Username));
        }
        else
        {
            return BadRequest("Wrong crendentials");
        }
    }

    private async Task<bool> IsValidUsernameAndPassword(string username, string password)
    {
        var user = await _userManager.FindByEmailAsync(username);
        return await _userManager.CheckPasswordAsync(user, password);
    }

    private async Task<string> GenerateToken(string username)
    {
        var user = await _userManager.FindByEmailAsync(username);
        var roles = from ur in _context.UserRoles
                    join r in _context.Roles on ur.RoleId equals r.Id
                    where ur.UserId == user.Id
                    select new { ur.UserId, ur.RoleId, r.Name };

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(JwtRegisteredClaimNames.Nbf, new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds().ToString()),
            new Claim(JwtRegisteredClaimNames.Exp, new DateTimeOffset(DateTime.Now.AddDays(1)).ToUnixTimeSeconds().ToString())
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role.Name));
        }

        string key = _config.GetValue<string>("Secrets:SecurityKey");

        var token = new JwtSecurityToken(
            new JwtHeader(
                new SigningCredentials(
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                    SecurityAlgorithms.HmacSha256)),
            new JwtPayload(claims));

        string output = new JwtSecurityTokenHandler().WriteToken(token);

        return output;
    }
}