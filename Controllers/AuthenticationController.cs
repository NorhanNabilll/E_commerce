using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using AutoMapper;
using ECommerce.DTOs.Authentication;
using ECommerce.Helpers;
using ECommerce.Models;
using ECommerce.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;


namespace ECommerce.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {

        private readonly IUserRepository _userRepository;
        private readonly APIResponse _response;
        private readonly IMapper _mapper;
        private readonly UserManager<AppUser> _userManager;
        private readonly EmailService _emailService;


        public AuthenticationController(IUserRepository userRepository, APIResponse response,
            IMapper mapper, UserManager<AppUser> userManager, EmailService emailService)
        {
            _userRepository = userRepository;
            _response = response;
            _mapper = mapper;
            _userManager = userManager;
            _emailService = emailService;
        }

        [HttpPost("Register")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(APIResponse))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(APIResponse))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(APIResponse))]
        public async Task<ActionResult<APIResponse>> Register([FromBody] RegisterRequestDto request)
        {

            try
            {
                // Check if user already exists
                var existingUser = await _userRepository.GetUserByEmailAsync(request.Email);
                if (existingUser != null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("User is already exist.");
                    return BadRequest(_response);
                }

                // Create user
                var user = _mapper.Map<AppUser>(request);
                var createResult = await _userRepository.CreateUserAsync(user, request.Password);
                if (!createResult.Succeeded)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = createResult.Errors.Select(e => e.Description).ToList();
                    return BadRequest(_response);
                }

                // Add user to role
                var roleResult = await _userRepository.AddUserToRoleAsync(user, "Customer");
                if (!roleResult.Succeeded)
                {
                    await _userRepository.DeleteUserAsync(user.Id); // Rollback user creation
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = roleResult.Errors.Select(e => e.Description).ToList();
                    return BadRequest(_response);
                }

                // Temporarily comment out email confirmation logic
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var encodedToken = Uri.EscapeDataString(token);

                var confirmationLink = $"https://localhost:7090/api/Authentication/confirmemail?userId={user.Id}&token={encodedToken}";

                await _emailService.SendEmailAsync(
                    user.Email,
                    "Confirm Your Email",
                    $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 8px;'>
                        <h2 style='color: #333;'>Verify Your Email</h2>
                        <p style='font-size: 16px; color: #555;'>
                            Please confirm your account by clicking the button below:
                        </p>
                        <a href='{confirmationLink}' style='display: inline-block; padding: 12px 20px; margin-top: 15px; background-color: #28a745; color: #fff; text-decoration: none; border-radius: 5px; font-weight: bold;'>
                            Confirm Email
                        </a>
                        <p style='margin-top: 20px; font-size: 14px; color: #999;'>
                            If you did not register, please ignore this email.
                        </p>
                        <hr style='margin: 30px 0;' />
                        <p style='font-size: 12px; color: #aaa;'>&copy; 2025 GoGreen Store. All rights reserved.</p>
                    </div>
                    ");

                _response.StatusCode = HttpStatusCode.OK;
                _response.Message = "Registerd Successfuly! Please Login";
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { ex.Message };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }
   
        
        [HttpGet("confirmemail")]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return BadRequest("Invalid user.");

            var decodedToken = Uri.UnescapeDataString(token);

            var result = await _userManager.ConfirmEmailAsync(user, decodedToken);

            if (result.Succeeded)
                return Ok("Email confirmed successfully!");

            return BadRequest("Email confirmation failed.");
        }


        [HttpPost("Login")]
        public async Task<ActionResult<APIResponse>> Login([FromBody] loginRequestDto request)
        {
            try
            {
                // Check if the email is registered before
                var user = await _userRepository.GetUserByEmailAsync(request.Email);
                if (user == null)
                {
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { "User not found." };
                    return Unauthorized(_response);
                }

                // ✅ Check if email is confirmed
                if (!await _userManager.IsEmailConfirmedAsync(user))
                {
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { "Please confirm your email before logging in." };
                    return Unauthorized(_response);
                }

                // Check if the password is correct for this email
                var isValidPassword = await _userRepository.CheckPasswordAsync(user, request.Password);
                if (!isValidPassword)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages.Add("Incorrect Email or Password");
                    return BadRequest(_response);
                }

                // Generate a new token
                var token = await _userRepository.CreateJwtToken(user);
                var role = await _userRepository.GetRoleAsync(user);

                object loginResponse;
                if (role == "Customer")
                {
                    loginResponse = new LoginResponseDto<UserDTO>
                    {
                        User = _mapper.Map<UserDTO>(user),
                        Token = token,
                        Role = "Customer"
                    };
                }
                else
                {
                    loginResponse = new LoginResponseDto<UserDTO>
                    {
                        User = _mapper.Map<UserDTO>(user),
                        Token = token,
                        Role = "Admin"
                    };
                }

                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                _response.Message = "Login successful";
                _response.Result = loginResponse;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { ex.Message };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }


    }
}
