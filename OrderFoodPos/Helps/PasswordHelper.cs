using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace OrderFoodPos.Services
{
	public static class PasswordHelper
	{
		public static string GenerateSalt()
		{
			var rng = new RNGCryptoServiceProvider();
			var saltBytes = new byte[16];
			rng.GetBytes(saltBytes);
			return Convert.ToBase64String(saltBytes);
		}

		public static string HashPassword(string password, string salt)
		{
			var combined = Encoding.UTF8.GetBytes(password + salt);
			using (SHA256 sha256 = SHA256.Create())
			{
				var hashBytes = sha256.ComputeHash(combined);
				return Convert.ToBase64String(hashBytes);
			}
		}

		public static bool VerifyPassword(string password, string hash, string salt)
		{
			string hashToCompare = HashPassword(password, salt);
			return hash == hashToCompare;
		}

	}
}
