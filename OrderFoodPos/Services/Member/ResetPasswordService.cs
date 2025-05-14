using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using OrderFoodPos.Repositories.Member;
using OrderFoodPos.Services.Member;

namespace OrderFoodPos.Services.Member
{
	/// <summary>
	/// 忘記密碼邏輯處理（針對 Member 表）
	/// </summary>
	public class ResetPasswordService
	{
		private readonly ResetPasswordRepository _resetPasswordRepository;
		private readonly EmailService _emailService;

		public ResetPasswordService(ResetPasswordRepository resetPasswordRepository, EmailService emailService)
		{
			_resetPasswordRepository = resetPasswordRepository;
			_emailService = emailService;
		}

		/// <summary>
		/// 處理忘記密碼流程，寄送重設連結到 Email
		/// </summary>
		public async Task<bool> ProcessForgotPasswordAsync(string email)
		{
			var member = await _resetPasswordRepository.GetUserByEmailAsync(email);
			if (member == null)
				return false;

			string userName = member.FullName ?? "會員";

			string token = GenerateToken();
			DateTime expirationTime = DateTime.Now.AddHours(1);

			await _resetPasswordRepository.SaveResetTokenAsync(email, token, expirationTime);

			string formattedExpiration = expirationTime.ToString("yyyy-MM-dd HH:mm");
			string resetLink = $"https://yochung-medical.vercel.app/reset?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";

			string emailBody = $@"
                <p>親愛的 {userName}，您好：</p>
                <p>我們收到您的重設密碼申請，請點擊以下連結：</p>
                <p>
                    <a href='{resetLink}' style='color: #007bff; font-weight: bold; text-decoration: none;'>
                        立即重設密碼
                    </a>
                </p>
                <p>此連結將在 1 小時內失效（至 {formattedExpiration}）。</p>
                <p>若您未發起此請求，請忽略本郵件。</p>
                <p>祐強醫療儀器團隊敬上</p>";

			await _emailService.SendEmailAsync(email, "重設您的密碼", emailBody);
			return true;
		}

		/// <summary>
		/// 驗證 Token 是否有效
		/// </summary>
		public async Task<bool> ValidateResetTokenAsync(string email, string token)
		{
			var validToken = await _resetPasswordRepository.GetResetTokenAsync(email, token);
			return validToken != null;
		}

		/// <summary>
		/// 重設密碼
		/// </summary>
		public async Task<bool> ResetPasswordAsync(string email, string token, string newPassword)
		{
			var member = await _resetPasswordRepository.GetUserByEmailAsync(email);
			if (member == null)
				return false;

			var validToken = await _resetPasswordRepository.GetResetTokenAsync(email, token);
			if (validToken == null)
				return false;

			string salt = GenerateSalt();
			string hashedPassword = HashPassword(newPassword, salt);

			var result = await _resetPasswordRepository.UpdatePasswordAsync(email, hashedPassword, salt);
			if (result <= 0)
				return false;

			await _resetPasswordRepository.MarkTokenAsUsedAsync(email, token);
			return true;
		}

		/// <summary>
		/// 產生隨機 salt
		/// </summary>
		private string GenerateSalt()
		{
			byte[] saltBytes = new byte[16];
			using var rng = RandomNumberGenerator.Create();
			rng.GetBytes(saltBytes);
			return Convert.ToBase64String(saltBytes);
		}

		/// <summary>
		/// 雜湊密碼 + salt（SHA256）
		/// </summary>
		private string HashPassword(string password, string salt)
		{
			using var sha256 = SHA256.Create();
			var combined = Encoding.UTF8.GetBytes(password + salt);
			var hash = sha256.ComputeHash(combined);
			return Convert.ToBase64String(hash);
		}

		/// <summary>
		/// 產生 Token（Base64）
		/// </summary>
		private string GenerateToken()
		{
			byte[] tokenData = new byte[32];
			using var rng = RandomNumberGenerator.Create();
			rng.GetBytes(tokenData);
			return Convert.ToBase64String(tokenData);
		}
	}
}
