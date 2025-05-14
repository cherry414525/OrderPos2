using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace OrderFoodPos
{
	public static class JwtHelper
	{
		private static readonly string SecretKey = "OrderFoosPosSecretKey2025@JWTAuth!"; //  替換成更安全的密鑰
		private static readonly string Issuer = "OrderFoosPos";
		private static readonly string Audience = "OrderFoosPosUser";

		public static string GenerateToken(string username, string fullName, string email, bool isAdmin, int storeId, string storeName)
		{
			var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));
			var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

			var claims = new[]
			{
		new Claim("username", username),
		new Claim("fullName", fullName),
		new Claim("email", email),
		new Claim("admin", isAdmin.ToString().ToLower()),
		new Claim("storeId", storeId.ToString()),
		new Claim("storeName", storeName ?? ""),
		new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
	};

			var token = new JwtSecurityToken(
				issuer: Issuer,
				audience: Audience,
				claims: claims,
				expires: DateTime.UtcNow.AddHours(1),
				signingCredentials: creds
			);

			return new JwtSecurityTokenHandler().WriteToken(token);
		}

	}
}
