using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using NodaTime;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Szlem.AspNetCore.Contracts.Identity;
using Szlem.AspNetCore.Options;
using Szlem.Domain;
using Szlem.Engine;
using Szlem.Models.Users;

namespace Szlem.AspNetCore.Infrastructure
{
    public interface IIdentityService
    {
        Task<Result<string, Error>> Register(Register.Request request);
        Task<Result<string, Error>> Login(Login.Request request);
    }

    class IdentityService : IIdentityService
    {
        private readonly JwtOptions _jwtOptions;
        private readonly IClock _clock;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IUserClaimsPrincipalFactory<ApplicationUser> _claimsPrincipalFactory;

        public IdentityService(
            JwtOptions jwtOptions,
            IClock clock,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IUserClaimsPrincipalFactory<ApplicationUser> claimsPrincipalFactory)
        {
            _jwtOptions = jwtOptions ?? throw new ArgumentNullException(nameof(jwtOptions));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
            _claimsPrincipalFactory = claimsPrincipalFactory ?? throw new ArgumentNullException(nameof(claimsPrincipalFactory));
        }

        public async Task<Result<string, Error>> Register(Register.Request request)
        {
            var user = new ApplicationUser { UserName = Guid.NewGuid().ToString(), Email = request.Email.ToString() };
            var result = await _userManager.CreateAsync(user, request.Password.ToString());

            if (result.Succeeded)
                return Result.Success<string, Error>(await GenerateSerializedSecurityTokenForUserWithEmail(request.Email));
            else
                return Result.Failure<string, Error>(new Error.BadRequest(string.Join(", ", result.Errors.Select(x => x.Description))));
        }

        public async Task<Result<string, Error>> Login(Login.Request request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email.ToString());
            if (user == null)
                return Result.Failure<string, Error>(new Error.BadRequest("Invalid login attempt"));

            var result = await _signInManager.PasswordSignInAsync(user, request.Password, isPersistent: false, lockoutOnFailure: false);

            if (result.Succeeded == false)
                return Result.Failure<string, Error>(new Error.BadRequest("Invalid login attempt"));

            return Result.Success<string, Error>(await GenerateSerializedSecurityTokenForUserWithEmail(request.Email));
        }


        #region supporting methods for token creation
        private async Task<string> GenerateSerializedSecurityTokenForUserWithEmail(EmailAddress email)
        {
            var token = await GenerateSecurityTokenForUserWithEmail(email);
            var handler = new JwtSecurityTokenHandler();
            return handler.WriteToken(token);
        }

        private async Task<SecurityToken> GenerateSecurityTokenForUserWithEmail(EmailAddress email)
        {
            var user = await _userManager.FindByEmailAsync(email.ToString());
            if (user is null)
                throw new ApplicationException("Cannot find user");

            var handler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtOptions.Secret);
            
            var tokenDescriptor = new SecurityTokenDescriptor()
            {
                Subject = new ClaimsIdentity(await GetValidClaims(user)),
                Expires = _clock.GetCurrentInstant().Plus(_jwtOptions.TokenLifetime).ToDateTimeUtc(),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            return handler.CreateToken(tokenDescriptor);
        }

        private async Task<IReadOnlyList<Claim>> GetValidClaims(ApplicationUser user)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            var claimsPrincipal = await _claimsPrincipalFactory.CreateAsync(user);
            claims.AddRange(claimsPrincipal.Identities.SelectMany(x => x.Claims));

            return claims;
        }
        #endregion

    }
}
