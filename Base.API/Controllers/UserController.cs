using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Base.API.Data.Repositories;
using Base.API.Models;
using Base.API.Models.Responses;
using Base.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Base.API.Controllers
{
    [Route("api/v1/users")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IOptions<TokenService> _jwtAuthentication;
        private readonly UsersRepository _usersRepository;

        public UserController(UsersRepository usersRepository, IOptions<TokenService> jwtAuthentication)
        {
            _usersRepository = usersRepository;
            _jwtAuthentication = jwtAuthentication ?? throw new ArgumentException(null, nameof(jwtAuthentication));
        }
        
        [HttpGet]
        public async Task<ActionResult> Get()
        {
            var users = await _usersRepository.GetAllUsersAsync();
            return Ok(users);
        }

        [HttpGet("/api/v1/user/{email}")]
        public async Task<ActionResult> Get(string email)
        {
            var user = await _usersRepository.GetUserAsync(email);
            
            if (user == null)
                return NotFound(new {message = "Usuário não encontrado"});
            
            user.AuthToken = _jwtAuthentication.Value.GenerateToken(user);
            return Ok(user);
        }
        
        [HttpPost("/api/v1/user/register")]
        public async Task<ActionResult<dynamic>> CreateUser([FromBody] User user)
        {
            Dictionary<string, string> errors = new Dictionary<string, string>();
            if(user.Name.Length < 3)
                errors.Add("nome", "Seu nome de usuário deve conter pelo menos 3 caracteres");
            if(user.Password.Length < 8)
                errors.Add("senha", "Sua senha deve conter pelo menos 8 carcteres");
            if (errors.Count > 0)
                return BadRequest(new { error = errors });
            var response = await _usersRepository.CreateUserAsync(user.Name, user.Email, user.Password);
            if (response.User != null) response.User.AuthToken = _jwtAuthentication.Value.GenerateToken(response.User);
            if (!response.Success)
                return BadRequest(new { error = response.ErrorMessage });
            return Ok(response.User);
        }

        [HttpPost("/api/v1/user/login")]
        public async Task<ActionResult> Login([FromBody] User user)
        {
            user.AuthToken = _jwtAuthentication.Value.GenerateToken(user);
            var result = await _usersRepository.LoginUserAsync(user);
            return result.User != null ? Ok(new UserResponse(result.User)) : Ok(result);
        }

        [HttpPost("/api/v1/user/logout")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<ActionResult> Logout()
        {
            var email = GetUserEmailFromToken(Request);
            if (email.StartsWith("Error")) return BadRequest(email);

            var result = await _usersRepository.LogoutUserAsync(email);
            return Ok(result);
        }
        
        
        [HttpDelete("/api/v1/user/delete")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<ActionResult> Delete([FromBody] PasswordBbject content)
        {
            var email = GetUserEmailFromToken(Request);
            if (email.StartsWith("Error")) return BadRequest(email);

            var user = await _usersRepository.GetUserAsync(email);
            if (!PasswordHash.Verify(content.Password, user.HashedPassword))
                return BadRequest("A senha fornecida não corresponde com a senha do usuário");

            return Ok(await _usersRepository.DeleteUserAsync(email));
        }
        
        private static string GetUserEmailFromToken(HttpRequest request)
        {
            var bearer = request.Headers.ToArray().First(h => h.Key == "Authorization")
                .Value.First().Substring(7);

            var jwtHandler = new JwtSecurityTokenHandler();
            var readableToken = jwtHandler.CanReadToken(bearer);
            if (readableToken != true) return "Error: no bearer in the header";

            var token = jwtHandler.ReadJwtToken(bearer);
            var claims = token.Claims;

            var userEmailClaim = claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email);

            return userEmailClaim == null ? "Error: Token does not contaim an email claim." : userEmailClaim.Value;
        }
        
        public class PasswordBbject
        {
            public string Password { get; set; }
        }
    }
}
