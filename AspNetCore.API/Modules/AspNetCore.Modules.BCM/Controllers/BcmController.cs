using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace AspNetCore.Modules.BCM.Controllers
{
    [ApiVersion("1.0")]
    [ApiVersion("2.0")]
    [ApiVersion("3.0")]
    [ApiVersion("4.0")]
    [ApiVersion("5.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Produces("application/json")]
    [ApiController]
    public partial class BcmController : Controller
    {
        private readonly string connectionString;
        private readonly IConfiguration configuration;
        private readonly IHttpClientFactory httpClientFactory;

        private readonly IMemoryCache memoryCache;

        private IHostingEnvironment hostingEnvironment;

        public BcmController(IConfiguration configuration, IHttpClientFactory httpClientFactory, IMemoryCache memoryCache, IHostingEnvironment hostingEnvironment)
        {
            this.configuration = configuration;
            this.connectionString = configuration.GetConnectionString("DefaultConnection");

            this.httpClientFactory = httpClientFactory;

            this.hostingEnvironment = hostingEnvironment;

            this.memoryCache = memoryCache;
        }


        private string GenerateToken(string email)
        {
            var serverSecret = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(this.configuration["JWT:ServerSecret"]));

            var now = DateTime.UtcNow;
            var issuer = this.configuration["JWT:Issuer"];
            var audience = this.configuration["JWT:Audience"];
            var identity = new ClaimsIdentity();
            identity.AddClaims(new[]
                {
                    new Claim(JwtRegisteredClaimNames.Sub, email),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(ClaimTypes.NameIdentifier, email),

                }
            );

            var expires = DateTime.Now.AddMinutes(Convert.ToDouble(this.configuration["JWT:ExpireMinutes"]));

            var signingCredentials = new SigningCredentials(serverSecret, SecurityAlgorithms.HmacSha256);
            var handler = new JwtSecurityTokenHandler();
            var token = handler.CreateJwtSecurityToken(issuer, audience, identity, now, expires, now, signingCredentials);
            var encodedJwt = handler.WriteToken(token);
            return encodedJwt;
        }

        private bool ValidateSecret(string value)
        {
            return value.Equals("12C1F7EF9AC8E288FBC2177B7F54D", StringComparison.OrdinalIgnoreCase);
        }

        [HttpGet]
        [Produces("application/json")]
        [ProducesResponseType(200)]
        [AllowAnonymous]
        [EnableCors("Allow-All")]
        public IActionResult Get()
        {
            var response = new
            {
                ok = true,
            };

            return new OkObjectResult(response);
        }
    }
}