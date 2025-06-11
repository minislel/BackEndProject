using Infrastructure.EF;
using JWT.Algorithms;
using JWT.Builder;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WebApi.Dto;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController(SignInManager<UserEntity> signInManager, UserManager<UserEntity> userManager, JwtSettings jwtSettings) : Controller
    {
        /// <summary>
        /// Authenticates a user and returns a JWT token if credentials are valid.
        /// </summary>
        /// <param name="dto">login params.</param>
        /// <response code="200">Returns token JWT.</response>
        /// <response code="404">user not found.</response>
        /// <response code="401">wrong password.</response>
        /// <response code="500">internal server error.</response>
        [HttpPost("login")]
        [AllowAnonymous]
        
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task< IActionResult> Login(LoginDto dto)
        {
            var user = await userManager.FindByNameAsync(dto.Login);
            if (user == null)
            {
                return NotFound(new { error = "Invalid user or password" });
            }
            var result = await  signInManager.CheckPasswordSignInAsync(user, dto.Password, false);
            if(result.Succeeded)
            {
                //zwrot tokenu
                return Ok(new {token = CreateToken(user) });
            }
            else
            {
                return Unauthorized(new {error = "Invalid user or password"});
            }
        }
        private string CreateToken(UserEntity user)
        {
            return new JwtBuilder()
                .WithAlgorithm(new HMACSHA256Algorithm())
                .WithSecret(Encoding.UTF8.GetBytes(jwtSettings.Secret))
                .AddClaim(JwtRegisteredClaimNames.Name, user.UserName)
                .AddClaim(JwtRegisteredClaimNames.Gender, "male")
                .AddClaim(JwtRegisteredClaimNames.Email, user.Email)
                .AddClaim(JwtRegisteredClaimNames.Exp, DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds())
                .AddClaim(JwtRegisteredClaimNames.Jti, Guid.NewGuid())
                .Audience(jwtSettings.Audience)
                .Issuer(jwtSettings.Issuer)
                .Encode();
        }


    }
}
