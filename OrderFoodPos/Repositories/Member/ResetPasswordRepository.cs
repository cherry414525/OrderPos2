using Dapper;
using System;
using System.Data;
using System.Threading.Tasks;
using OrderFoodPos.Models.Member;

namespace OrderFoodPos.Repositories.Member
{
	/// <summary>
	/// 忘記密碼處理：針對 Member 表做查詢與密碼更新
	/// </summary>
	public class ResetPasswordRepository : BaseRepository<StoreAccountModel>
	{
		public ResetPasswordRepository(IDbConnection dbConnection) : base(dbConnection) { }

		/// <summary>
		/// 根據 Email 查詢使用者資料（Id、Email、FullName）
		/// </summary>
		public async Task<StoreAccountModel?> GetUserByEmailAsync(string email)
		{
			string sql = @"
                SELECT Id, Email, FullName 
                FROM Member 
                WHERE Email = @Email";

			return await GetAsync<StoreAccountModel>(sql, new { Email = email }); // 使用 BaseRepository 泛型查詢
		}

		/// <summary>
		/// 儲存密碼重設 Token
		/// </summary>
		public async Task<int> SaveResetTokenAsync(string email, string token, DateTime expirationTime)
		{
			string sql = @"
                INSERT INTO PasswordResetTokens 
                    (Email, Token, ExpirationTime, IsUsed, CreatedAt)
                VALUES 
                    (@Email, @Token, @ExpirationTime, 0, GETDATE())";

			return await AddAsync(sql, new
			{
				Email = email,
				Token = token,
				ExpirationTime = expirationTime
			});
		}

		/// <summary>
		/// 驗證 Token 是否存在且有效
		/// </summary>
		public async Task<string?> GetResetTokenAsync(string email, string token)
		{
			string sql = @"
                SELECT Token 
                FROM PasswordResetTokens 
                WHERE Email = @Email 
                  AND Token = @Token 
                  AND ExpirationTime > GETDATE() 
                  AND IsUsed = 0";

			return await ExecuteScalarAsync<string>(sql, new { Email = email, Token = token });
		}

		/// <summary>
		/// 更新密碼與加鹽資訊
		/// </summary>
		public async Task<int> UpdatePasswordAsync(string email, string hashedPassword, string salt)
		{
			string sql = @"
                UPDATE Member
                SET PasswordHash = @PasswordHash,
                    Salt = @Salt,
                    PasswordChangeDate = GETDATE(),
                    UpdatedDate = GETDATE()
                WHERE Email = @Email";

			return await UpdateAsync(sql, new
			{
				PasswordHash = hashedPassword,
				Salt = salt,
				Email = email
			});
		}

		/// <summary>
		/// 標記 Token 為已使用
		/// </summary>
		public async Task<int> MarkTokenAsUsedAsync(string email, string token)
		{
			string sql = @"
                UPDATE PasswordResetTokens 
                SET IsUsed = 1 
                WHERE Email = @Email AND Token = @Token";

			return await UpdateAsync(sql, new { Email = email, Token = token });
		}

		/// <summary>
		/// 刪除過期或已使用的 Token
		/// </summary>
		public async Task<int> DeleteExpiredTokensAsync()
		{
			string sql = @"
                DELETE FROM PasswordResetTokens 
                WHERE ExpirationTime <= GETDATE() OR IsUsed = 1";

			return await ExecuteAsync(sql);
		}
	}
}
