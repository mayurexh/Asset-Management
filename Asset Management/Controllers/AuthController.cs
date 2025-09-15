using Asset_Management.Database;
using Asset_Management.DTO;
using Asset_Management.Models;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Asset_Management.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : Controller
    {
        private readonly AssetDbContext _dbContext;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IConfiguration _configuration;
        public AuthController(AssetDbContext dbContext, IPasswordHasher<User> passwordHasher, IConfiguration configuration)
        {
            _dbContext = dbContext;
            _passwordHasher = passwordHasher;
            _configuration = configuration;
        }

        [HttpPost("Register")]
        public IActionResult Register(RegisterDTO dto)
        {
            if (ModelState.ContainsKey("Email"))
            {
                foreach (var error in ModelState["Email"].Errors)
                {
                    return BadRequest(error.ErrorMessage);
                }
            }

            if (!ModelState.IsValid)
            {
                return BadRequest("Username can only contain letters, numbers, hyphens (-), and underscores (_) (upto 30 characters)");
            }
            //check if any user with similar username exists 
            if (_dbContext.Users.Any(u => u.Username == dto.Username))
                return BadRequest("Username already exists");

            if (_dbContext.Users.Any(u => u.Email == dto.Email))
                return BadRequest("User with same email already exist");

            var user = new User
            {
                Username = dto.Username,
                Email = dto.Email,
                Role = "Viewer",
                CreatedAtUtc = DateTime.UtcNow
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);

            _dbContext.Users.Add(user);
            _dbContext.SaveChanges();

            return Ok("User Registered");
        }

        [HttpPost("Login")]
        public IActionResult Login(LoginDTO dto)
        {

            //if (!ModelState.IsValid)
            //    return BadRequest("Username can only contain letters, numbers, hyphens (-), and underscores (_) (upto 30 characters)");
            // Find user by username
            var user = _dbContext.Users.FirstOrDefault(u => u.Username == dto.Username);
            if (user == null)
                return Unauthorized("Invalid username or password");

            // Verify password
            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
            if (result == PasswordVerificationResult.Failed)
                return Unauthorized("Invalid username or password");

            // Create claims
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role)
    };

            //generate JWT 
            var jwtSettings = _configuration.GetSection("Jwt"); //plain string 
            //jwt sign in algos need a cryptographic key object 
            //wrap key into a security key object that jwt can understand
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256); //header {type: jwt, algo: SHA256)

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(double.Parse(jwtSettings["ExpiresInMinutes"])),
                signingCredentials: creds
             );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token); //generates a token based on the creds and encrypts the payload

            //result = {Base64Url(Header)}.{Base64Url(Payload)}.{Base64Url(Signature)}
            //Signature is generated when WriteToken takes algo type and key from the creds and uses SHA256(header.payload,key);


            //Save the token to a Cookie for security
            Response.Cookies.Append("token", tokenString, new CookieOptions
            {
                HttpOnly = true, //token can't be accessed with js
                Secure = true,
                SameSite = SameSiteMode.None,
                Path = "/"

            });


            return Ok(new
            {
                Token = tokenString,
                ExpiresAt = DateTime.UtcNow.AddMinutes(double.Parse(jwtSettings["ExpiresInMinutes"]))
            });


        }

        [HttpGet("GetUserInfo")]
        [Authorize]
        public async Task<IActionResult> GetUserInfo()
        {

            var userClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;

            if(userClaim == null)
                return Unauthorized("User ID claim not found in token.");

            int userId = int.Parse(userClaim);
            var user = _dbContext.Users.FirstOrDefault(u => u.Id == userId);
            if (user == null)
                return NotFound("User not found");
            Console.WriteLine("FROM GET USER INFO");
            Console.WriteLine(user.Username);
            int id = user.Id;
            string Username = user.Username;
            string Role = user.Role;

            return Ok( new {
                id = id,
                username = Username,
                role = Role
            });


        }
        [HttpPost("Logout")]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("token", new CookieOptions
            {
                HttpOnly = true, //token can't be accessed with js
                Secure = true,
                SameSite = SameSiteMode.None,
                Path = "/"
            });
            return Ok("Logged Out");
        }


        [HttpPost("ExternalLogin")]
        public async Task<IActionResult> ExternalLogin([FromBody] ExternalLoginDTO dto)
        {

            Console.WriteLine("Controller started");
            if (string.IsNullOrEmpty(dto.IdToken))
                return BadRequest("ID Token is required");

            GoogleJsonWebSignature.Payload payload;
            try
            {
                // Verify the Google ID token
                payload = await GoogleJsonWebSignature.ValidateAsync(dto.IdToken);
                // payload contains email, name, sub (Google user ID)

                
            }
            catch
            {
                return Unauthorized("Invalid Google ID Token");
            }

            // Check if user already exists
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == payload.Email);


            if (user == null)
            {
                // Create new user
                user = new User
                {
                    Username = payload.Name ?? payload.Email.Split('@')[0],
                    Email = payload.Email,
                    Role = "Viewer",
                    PasswordHash = null,
                    CreatedAtUtc = DateTime.UtcNow
                };

                _dbContext.Users.Add(user);
                await _dbContext.SaveChangesAsync();
            }

            // Generate claims for your custom JWT
            var claims = new[]
            {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role)
};

            // Generate JWT
            var jwtSettings = _configuration.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(double.Parse(jwtSettings["ExpiresInMinutes"])),
                signingCredentials: creds
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            //Save the token to a Cookie for security
            Response.Cookies.Append("token", tokenString, new CookieOptions
            {
                HttpOnly = true, //token can't be accessed with js
                Secure = true,
                SameSite = SameSiteMode.None,
                Path = "/"

            });

            // Return the token + user info to frontend
            return Ok(new
            {
                Token = tokenString,
                ExpiresAt = DateTime.UtcNow.AddMinutes(double.Parse(jwtSettings["ExpiresInMinutes"])),
                User = new
                {
                    id = user.Id,
                    username = user.Username,
                    role = user.Role
                }
            });
        }




    }


}
