using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MedicalTrackingSystem.Data;
using MedicalTrackingSystem.Models;
using System.Security.Cryptography;
using MedicalTrackingSystem.Controllers;

namespace MedicalTrackingSystem.Services
{
    public interface IAuthService
    {
        Task<(User user, string token)> AuthenticateAsync(string username, string password);
        Task<bool> RegisterAsync(RegisterModel model);
        string GenerateJwtToken(User user);
        bool VerifyPassword(string password, string hashedPassword);
    }

    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthService(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<(User user, string token)> AuthenticateAsync(string username, string password)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null || !VerifyPassword(password, user.Password))
            {
                return (null, null);
            }

            // Check if the user exists in the corresponding role table
            bool isValidUserType = false;
            if (user.UserType == "Doctor")
            {
                isValidUserType = await _context.Doctors.AnyAsync(d => d.UserId == user.UserId);
            }
            else if (user.UserType == "Patient")
            {
                isValidUserType = await _context.Patients.AnyAsync(p => p.UserId == user.UserId);
            }

            if (!isValidUserType)
            {
                return (null, null);
            }

            var token = GenerateJwtToken(user);

            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return (user, token);
        }

        public async Task<bool> RegisterAsync(RegisterModel model)
        {
            if (await _context.Users.AnyAsync(u => u.Username == model.Username))
            {
                throw new Exception("Username is already taken");
            }

            if (await _context.Users.AnyAsync(u => u.Email == model.Email))
            {
                throw new Exception("Email is already registered");
            }

            var user = new User
            {
                Username = model.Username,
                Password = BCrypt.Net.BCrypt.HashPassword(model.Password),
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                UserType = model.UserType,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            if (model.UserType == "Patient")
            {
                var patient = new Patient
                {
                    UserId = user.UserId,
                    User = user
                };
                _context.Patients.Add(patient);
            }
            else if (model.UserType == "Doctor" && model.ClinicId.HasValue)
            {
                var doctor = new Doctor
                {
                    UserId = user.UserId,
                    User = user,
                    ClinicId = model.ClinicId.Value
                };
                _context.Doctors.Add(doctor);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Role, user.UserType),
                    new Claim(ClaimTypes.GivenName, user.FirstName),
                    new Claim(ClaimTypes.Surname, user.LastName)
                }),
                Expires = DateTime.UtcNow.AddHours(24),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public bool VerifyPassword(string password, string hashedPassword)
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }

        private string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }
    }
} 