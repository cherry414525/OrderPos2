using Microsoft.IdentityModel.Tokens;
using OrderFoodPos.Models.Member;
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

		public static string GenerateToken(string username, string fullName, string email, bool isAdmin, string storeName)
		{
			var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));
			var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

			var claims = new[]
			{
		new Claim("username", username),
		new Claim("fullName", fullName),
		new Claim("email", email),
		new Claim("admin", isAdmin.ToString().ToLower()),
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


		//給顧客的加密
		public static string GenerateCustomerToken(StoreCustomer customer)
		{
			var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));
			var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

			var claims = new[]
			{
		new Claim("customerId", customer.Id.ToString()),
		new Claim("storeId", customer.StoreId.ToString()),
		new Claim("name", customer.Name ?? ""),
		new Claim("phone", customer.PhoneNumber ?? ""),
		new Claim("lineId", customer.LineId ?? ""),
		new Claim("email", customer.Email ?? ""),
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


		/// <summary>
		/// 生成訂單的 JWT Token
		/// </summary>
		/// <param name="storeId"></param>
		/// <returns></returns>
		public static string GenerateOrderUrlToken(int storeId, int hours)
		{
			var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));
			var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

			var claims = new[]
			{
		new Claim("storeId", storeId.ToString()),
		new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
		new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
	};

			DateTime? expires = hours > 0 ? DateTime.UtcNow.AddHours(hours) : null;

			var token = new JwtSecurityToken(
				issuer: Issuer,
				audience: Audience,
				claims: claims,
				expires: expires, // null = 不設定過期時間 = 永久有效
				signingCredentials: creds
			);

			return new JwtSecurityTokenHandler().WriteToken(token);
		}



	}
}
