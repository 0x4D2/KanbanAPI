using KanbanAPI.Data;
using KanbanAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace KanbanAPI.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class UserController : Controller
    {
        private readonly KanbanDbContext _context;

        public UserController(KanbanDbContext context)
        {
            _context = context;
        }

        //Edite password
        [HttpPut("users/{id}/password")]
        public async Task<IActionResult> ChangePassword(int id, [FromBody] PasswordUpdateModel model)
        {
     
                var user = await _context.Users.FindAsync(id);

                // If the user doesn't exist, return a 404 Not Found response
                if (user == null)
                {
                    return NotFound();
                }

                // If the current user is not an admin and is not the user being edited, return a 401 Unauthorized response
                //if (!User.IsInRole("Admin") && user.Id != GetCurrentUserId())
                //{
                //    return Unauthorized();
                //}

                // Verify the current password before changing it
                byte[] salt = Encoding.ASCII.GetBytes("this is the salt");
                int iterations = 10000;

                byte[] hash = KeyDerivation.Pbkdf2(model.NewPassword, salt, KeyDerivationPrf.HMACSHA256, iterations, 256 / 8);

                bool verified = user.Password == Convert.ToBase64String(hash);

                if (verified)
                {
                    return BadRequest("The new password cannot be the same as the current password");
                }

                // Calculate the hash of the new password
                byte[] newSalt = Encoding.ASCII.GetBytes("this is the salt");
                byte[] newPwdHash = KeyDerivation.Pbkdf2(model.NewPassword, newSalt, KeyDerivationPrf.HMACSHA256, iterations, 256 / 8);
                user.Password = Convert.ToBase64String(newPwdHash);

                _context.Users.Update(user);
                await _context.SaveChangesAsync();


                return NoContent();
            
        }

        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
        }


        //GET
        [HttpGet]
        public JsonResult GetUser(int userId)
        {
           
                var result = _context.Users.Find(userId);

                if (result == null)
                    return new JsonResult(NotFound());

               return new JsonResult(Ok(result));
            
        }

        [HttpDelete]
        public JsonResult DeleteUser(int userId)
        {
           
                var result = _context.Users.Find(userId);

                if (result == null)
                    return new JsonResult(NotFound());

                 _context.Users.Remove(result);
                 _context.SaveChanges();

                return new JsonResult(Ok($"User with Id: {userId} was successfully Removed"));
            
        }

        [HttpGet]
        public IActionResult GetAllUsers()
        {
            var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            var handler = new JwtSecurityTokenHandler();
            var tokenS = handler.ReadJwtToken(token);

            var isAdmin = tokenS.Claims.Any(c => c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role" && c.Value == "Admin");

            if (!isAdmin)
            {
                return Unauthorized();
            }

            var users = _context.Users.ToList();
            return new JsonResult(Ok(users));
        }


    }
}