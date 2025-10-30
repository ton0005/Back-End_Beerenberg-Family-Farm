using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using FarmManagement.Api.ApiModels;
using FarmManagement.Application.Services;
using FarmManagement.Application.Repositories;
using FarmManagement.Core.Entities.Identity;

namespace FarmManagement.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LoginController : ControllerBase
    {
        private readonly AuthService _authService;
        private readonly IJwtTokenService _jwt;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public LoginController(AuthService authService, IJwtTokenService jwt,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _authService = authService;
            _jwt = jwt;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        /// <summary>Login</summary>
        /// <param name="req">email and password</param>
        /// <param name="ct">Cancellation Token</param>
        /// <response code="200">Successful login, returns message</response>
        /// <response code="401">Unauthorised/invalid credentails, returns a message</response>
        /// <response code="400">Bad request, invalid input format</response>
        /// <response code="500">Server error</response>
        /// <remarks>
        /// Sample request:
        ///     POST /api/login
        ///     {
        ///         "username": "admin",
        ///         "password": "password"
        ///     }
        /// 
        /// Sample sucessful response (200):
        ///     {
        ///         "username": "admin",
        ///         "token": "token"
        ///     }
        /// 
        /// Sample error response (401):
        ///     {
        ///         "message": "Invalid username or password"
        ///     }
        /// </remarks>
        [HttpPost]
        [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest req, CancellationToken ct = default)
        {
            // Try to sign in via Identity first (if user exists in Identity store)
            var identityUser = await _userManager.FindByEmailAsync(req.Username);
            if (identityUser != null)
            {
                // Debug: Log user info
                Console.WriteLine($"DEBUG: Found Identity user: {identityUser.Email}, StaffId: {identityUser.StaffId}");
                var roles = await _userManager.GetRolesAsync(identityUser);
                Console.WriteLine($"DEBUG: User roles: {string.Join(", ", roles)}");
                
                var result = await _signInManager.CheckPasswordSignInAsync(identityUser, req.Password, lockoutOnFailure: true);
                Console.WriteLine($"DEBUG: Password check result - Succeeded: {result.Succeeded}, IsLockedOut: {result.IsLockedOut}, IsNotAllowed: {result.IsNotAllowed}");
                
                if (!result.Succeeded) return Unauthorized(new LoginResponse(false, "", "Invalid username or password"));

                var token = _jwt.CreateToken(identityUser.Id, identityUser.Email ?? req.Username);
                return Ok(new LoginResponse(true, token, "Login success"));
            }

            // fallback to legacy auth service (database-stored AuthUser)
            Console.WriteLine($"DEBUG: Identity user not found for {req.Username}, trying legacy auth");
            var user = await _authService.LoginAsync(req.Username, req.Password, ct);
            if (user is null) 
            {
                Console.WriteLine($"DEBUG: Legacy auth also failed for {req.Username}");
                return Unauthorized(new LoginResponse(false, "", "Invalid username or password"));
            }

            Console.WriteLine($"DEBUG: Legacy auth succeeded for {req.Username}");
            if (user is null) return Unauthorized(new LoginResponse(false, "", "Invalid username or password"));

            var legacyToken = _jwt.CreateToken(user.Username, user.Email);
            return Ok(new LoginResponse(true, legacyToken, "Login success"));
        }
    }
}