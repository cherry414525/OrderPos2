using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OrderFoodPos.Models.Member
{
	public class ResetPassword
    {
		/// <summary>
		/// 忘記密碼請求 DTO
		/// </summary>
		public class ForgotPasswordRequest
		{
			[JsonPropertyName("email")]
			public string Email { get; set; }
		}

		/// <summary>
		/// 重設密碼請求 DTO
		/// </summary>
		public class ResetPasswordRequest
		{
			public string Email { get; set; }
			public string Token { get; set; } // 新增 Token 屬性
			public string NewPassword { get; set; }
		}

		// 驗證 Token 的 DTO
		public class ValidateTokenRequest
		{
			public string Email { get; set; }
			public string Token { get; set; }
		}
	}
}
