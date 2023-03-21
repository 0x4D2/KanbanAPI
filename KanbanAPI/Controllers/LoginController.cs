using KanbanAPI.Authentication;
using KanbanAPI.Data;
using KanbanAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Linq;

namespace KanbanAPI.Controllers
{
    public class LoginController : Controller
    {
        private readonly KanbanDbContext _context;

        public LoginController(KanbanDbContext context)
        {
            _context = context;
        }

        [HttpPut("api/edituser/{id}")]
        public IActionResult EditUser(int id, [FromBody] User updatedUser)
        {

            var user = _context.Users.FirstOrDefault(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            user.Username = updatedUser.Username;
            user.Email = updatedUser.Email;
            user.Role = updatedUser.Role;

            _context.SaveChanges();

            return Ok(user);

        }

        [AllowAnonymous]
        [Route("api/login")]
        [HttpPost]
        public IActionResult Login([FromBody] LoginModel model)
        {
            if (string.IsNullOrEmpty(model.Username) || string.IsNullOrEmpty(model.Password))
            {
                return BadRequest("Invalid login credentials");
            }

            User user;

            user = _context.Users.SingleOrDefault(u => u.Username == model.Username);


            if (user == null || string.IsNullOrEmpty(user.Password))
                return Unauthorized("Invalid credentials");


            byte[] salt = Encoding.ASCII.GetBytes("this is the salt");
            int iterations = 10000;

            byte[] hash = KeyDerivation.Pbkdf2(model.Password, salt, KeyDerivationPrf.HMACSHA256, iterations, 256 / 8);

            bool verified = user.Password == Convert.ToBase64String(hash);

            if (!verified)
            {
                return Unauthorized("Invalid credentials");
            }

            var token = GenerateAccessToken(user.Username, user.Role);

            var response = new
            {
                token = token
            };

            return Json(response);
        }

        [Route("api/register")]
        [HttpPost]
        public IActionResult Register([FromBody] User model)
        {
            if (string.IsNullOrEmpty(model.Username) || string.IsNullOrEmpty(model.Password) || string.IsNullOrEmpty(model.Email))
                return BadRequest("Invalid registration data");


            if (UserExists(model.Email, model.Username))
                return BadRequest("User already exists");


            byte[] salt = Encoding.ASCII.GetBytes("this is the salt");

            int iterations = 10000;
            byte[] hash = KeyDerivation.Pbkdf2(model.Password, salt, KeyDerivationPrf.HMACSHA256, iterations, 256 / 8);

            var user = new User
            {
                Username = model.Username,
                Password = Convert.ToBase64String(hash),
                Email = model.Email,
                Role = model.Role
            };



            _context.Users.Add(user);
            _context.SaveChanges();


            var token = GenerateAccessToken(model.Username, user.Role);
            return  Ok(token);
        }

        public IActionResult Authenticate([FromBody] LoginModel model)
        {
            if (string.IsNullOrEmpty(model.Username) || string.IsNullOrEmpty(model.Password))
            {
                return BadRequest("Invalid login data");
            }

            User user;
            using (var db = new KanbanDbContext())
            {
                user = db.Users.SingleOrDefault(u => u.Username == model.Username);
            }

            if (user == null || string.IsNullOrEmpty(user.Password))
            {
                return Unauthorized("Invalid credentials");
            }
            byte[] hash = Convert.FromBase64String(user.Password);
            byte[] salt = Encoding.ASCII.GetBytes("this is the salt");
            int iterations = 10000;
            bool verified = KeyDerivation.Pbkdf2(model.Password, salt, KeyDerivationPrf.HMACSHA256, iterations, 256 / 8) == hash;
            if (!verified)
            {
                return Unauthorized("Invalid credentials");
            }

            var token = GenerateAccessToken(user.Username, user.Role);
            return Ok(token);
        }

        private string GenerateAccessToken(string username, string userRole)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, userRole)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("my-secret-key-12345"));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: "https://localhost:5001",
                audience: "https://localhost:5001",
                claims: claims,
                expires: DateTime.Now.AddMinutes(30),
                signingCredentials: creds

            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private bool UserExists(string email, string username)
        {

            return _context.Users.Any(u => u.Email == email || u.Username == username);

        }
    }
}
