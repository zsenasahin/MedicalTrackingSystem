using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedicalTrackingSystem.Data;
using MedicalTrackingSystem.Models;
using MedicalTrackingSystem.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace MedicalTrackingSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuthService _authService;

        public AccountController(ApplicationDbContext context, IAuthService authService)
        {
            _context = context;
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = string.Join("; ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage));
                    return BadRequest($"Validation failed: {errors}");
                }

                if (await _context.Users.AnyAsync(u => u.Username == model.Username))
                    return BadRequest("Username already exists");

                if (await _context.Users.AnyAsync(u => u.TCIdentityNumber == model.TCIdentityNumber))
                    return BadRequest("TC Identity Number already exists");

                if (model.UserType == "Doctor" && !model.ClinicId.HasValue)
                    return BadRequest("Clinic selection is required for doctor registration");

                var user = new User
                {
                    Username = model.Username,
                    Password = BCrypt.Net.BCrypt.HashPassword(model.Password),
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Email = model.Email,
                    TCIdentityNumber = model.TCIdentityNumber,
                    UserType = model.UserType
                };

                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();

                    if (model.UserType == "Doctor")
                    {
                        var doctor = new Doctor
                        {
                            UserId = user.UserId,
                            ClinicId = model.ClinicId.Value
                        };
                        _context.Doctors.Add(doctor);
                    }
                    else if (model.UserType == "Patient")
                    {
                        var patient = new Patient
                        {
                            UserId = user.UserId
                        };
                        _context.Patients.Add(patient);
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Ok(new { message = "Registration successful" });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return BadRequest($"Database operation failed: {ex.Message}. Inner exception: {ex.InnerException?.Message}");
                }
            }
            catch (Exception ex)
            {
                return BadRequest($"Registration failed: {ex.Message}. Stack trace: {ex.StackTrace}");
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            try
            {
                // Check if the provided user type matches the requested login type
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == model.Username);
                if (user != null && user.UserType != model.UserType)
                {
                    return Unauthorized(new { message = $"This account is not registered as a {model.UserType}" });
                }

                var (authenticatedUser, token) = await _authService.AuthenticateAsync(model.Username, model.Password);
                if (authenticatedUser == null || string.IsNullOrEmpty(token))
                {
                    return Unauthorized(new { message = "Invalid username or password" });
                }

                // Create claims for the user
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, authenticatedUser.UserId.ToString()),
                    new Claim(ClaimTypes.Name, authenticatedUser.Username),
                    new Claim(ClaimTypes.Role, authenticatedUser.UserType),
                    new Claim(ClaimTypes.GivenName, authenticatedUser.FirstName),
                    new Claim(ClaimTypes.Surname, authenticatedUser.LastName)
                };

                // Create claims identity
                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                // Create authentication properties
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(24)
                };

                // Sign in the user
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                // Set JWT token in cookie
                Response.Cookies.Append("token", token, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.UtcNow.AddHours(24)
                });

                return Ok(new
                {
                    authenticatedUser.UserId,
                    authenticatedUser.Username,
                    authenticatedUser.FirstName,
                    authenticatedUser.LastName,
                    authenticatedUser.UserType,
                    Token = token
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }

    public class RegisterModel
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string TCIdentityNumber { get; set; }
        public string UserType { get; set; }
        public int? ClinicId { get; set; }
    }

    public class LoginModel
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string UserType { get; set; }
    }
} 