using System.Data;
using Dapper;
using Microsoft.AspNetCore.Connections;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using OrderFoodPos.Models.Member;
using OrderFoodPos.Services;



namespace OrderFoodPos.Repositories.Member
{
	public class StoreAccountRepository : BaseRepository<StoreAccountModel>
    {
		//public StoreAccountRepository(IDbConnection dbConnection) : base(dbConnection, connectionFactory.GetConnectionString()) { }
		public String _connectionString;

        public StoreAccountRepository(SqlConnectionFactory connectionFactory)
        : base(connectionFactory.CreateConnection()) 
		{
			_connectionString = connectionFactory.GetConnectionString();

        }


        /// <summary>
        /// 驗證帳密是否存在資料庫中
        /// </summary>
        public async Task<bool> IsLoginValidAsync(string username, string password)
		{
			string sql = "SELECT PasswordHash, Salt FROM StoreAccounts WHERE Username = @Username";
			var result = await GetAsync(sql, new { Username = username });

			if (result == null)
				return false;

			string inputHash = PasswordHelper.HashPassword(password, result.Salt);
			return inputHash == result.PasswordHash;
		}


		//註冊帳號並建立商店資料
		public async Task<RegisterStatus> RegisterAsync(string username, string password, string email, string fullName)
		{
			// 檢查帳號是否在 StoreAccounts 或 StoreEmployees 中已存在
			string sqlCheckUsername = @"
SELECT COUNT(1) FROM StoreAccounts WHERE Username = @Username
UNION ALL
SELECT COUNT(1) FROM StoreEmployees WHERE Username = @Username";

			var usernameCounts = await _dbConnection.QueryAsync<int>(sqlCheckUsername, new { Username = username });
			if (usernameCounts.Sum() > 0)
				return RegisterStatus.UsernameExists;

			// 檢查信箱是否在 StoreAccounts 或 StoreEmployees 中已存在
			string sqlCheckEmail = @"
SELECT COUNT(1) FROM StoreAccounts WHERE Email = @Email
UNION ALL
SELECT COUNT(1) FROM StoreEmployees WHERE Email = @Email";

			var emailCounts = await _dbConnection.QueryAsync<int>(sqlCheckEmail, new { Email = email });
			if (emailCounts.Sum() > 0)
				return RegisterStatus.EmailExists;

			// 雜湊密碼
			string salt = PasswordHelper.GenerateSalt();
			string hash = PasswordHelper.HashPassword(password, salt);

			// 建立新 StoreAccount 並取得新帳號 Id
			string sqlInsertAccount = @"
INSERT INTO StoreAccounts (Username, PasswordHash, Salt, Email, FullName, Admin, Locked, CreatedDate)
VALUES (@Username, @PasswordHash, @Salt, @Email, @FullName, 0, 1, GETDATE());
SELECT CAST(SCOPE_IDENTITY() AS int);";

			int newAccountId = await _dbConnection.ExecuteScalarAsync<int>(sqlInsertAccount, new
			{
				Username = username,
				PasswordHash = hash,
				Salt = salt,
				Email = email,
				FullName = fullName
			});

			// 建立商店資料
			string sqlInsertStore = @"
INSERT INTO Stores (StoreAccountsId, StoreName, Address, PhoneNumber, Locked, Opened, [Plan], CreatedDate)
VALUES (@StoreAccountsId, @StoreName, '', '', 0, 1, '基礎版', GETDATE());";

			await ExecuteAsync(sqlInsertStore, new
			{
				StoreAccountsId = newAccountId,
				StoreName = fullName + " 的店"
			});

			return RegisterStatus.Success;
		}




		public async Task<StoreAccountModel?> GetUserByUsernameAsync(string username)
		{
			Console.WriteLine($"查詢帳號：{username}"); // 看傳進來的是什麼
			string sql = "SELECT * FROM StoreAccounts WHERE Username = @Username"; // 查詢語法
			var result = await _dbConnection.QueryFirstOrDefaultAsync<StoreAccountModel>(sql, new { Username = username }); // 查詢
			return result;
		}

		public async Task<int?> GetMemberIdByUsername(string username)
		{
			string sql = "SELECT Id FROM StoreAccounts WHERE Username = @Username"; // 查詢會員 ID
			return await _dbConnection.QueryFirstOrDefaultAsync<int?>(sql, new { Username = username });
		}


		public async Task AddLoginHistoryAsync(string username, string ip, string deviceId, string browser, string os, string screen, string status, string errorMessage = null)
		{
			string sql = @"
		INSERT INTO StoreLoginHistory 
		(Username, LoginTime, IP, DeviceId, BrowserInfo, OSInfo, ScreenResolution, Status, ErrorMessage)
		VALUES 
		(@Username, GETDATE(), @IP, @DeviceId, @BrowserInfo, @OSInfo, @ScreenResolution, @Status, @ErrorMessage)";

			await ExecuteAsync(sql, new
			{
				Username = username,
				IP = ip,
				DeviceId = deviceId,
				BrowserInfo = browser,
				OSInfo = os,
				ScreenResolution = screen,
				Status = status,
				ErrorMessage = errorMessage
			});
		}

		//獲得全部會員資料
		public async Task<IEnumerable<StoreAccountModel>> GetAllMembersAsync()
		{
			string sql = "SELECT * FROM StoreAccounts ORDER BY CreatedDate DESC"; // 依建立時間排序
			return await _dbConnection.QueryAsync<StoreAccountModel>(sql);
		}

		//更新會員資料
		public async Task<bool> UpdateMemberAsync(StoreAccountModel model)
		{
			string sql = @"
		UPDATE StoreAccounts
		SET
			FullName = @FullName,
			Email = @Email,
			Phone = @Phone,
			Locked = @Locked,
			Admin = @Admin,
			UpdatedDate = GETDATE()
		WHERE Username = @Username
	";

			int affected = await ExecuteAsync(sql, model);
			return affected > 0;
		}

		public async Task<bool> InsertMemberAsync(StoreAccountModel model)
		{
			string sql = @"
        INSERT INTO StoreAccounts
        (Username, PasswordHash, Salt, Email, FullName, Phone, Locked, Admin, CreatedDate, UpdatedDate)
        VALUES
        (@Username, @PasswordHash, @Salt, @Email, @FullName, @Phone, @Locked, @Admin, @CreatedDate, @UpdatedDate)";

			int rows = await _dbConnection.ExecuteAsync(sql, model);
			return rows > 0;
		}


		public async Task<StoreAccountModel?> GetMemberByIdAsync(int id)
		{
			string sql = "SELECT * FROM StoreAccounts WHERE Id = @Id";
			return await _dbConnection.QueryFirstOrDefaultAsync<StoreAccountModel>(sql, new { Id = id });
		}

		public async Task<StoreModel?> GetStoreByAccountIdAsync(int accountId)
		{
			string sql = "SELECT Id, StoreAccountsId, StoreName FROM Stores WHERE StoreAccountsId = @AccountId";
			return await _dbConnection.QueryFirstOrDefaultAsync<StoreModel>(sql, new { AccountId = accountId });
		}

		//取得商家每天營業時間
        public async Task<IEnumerable<StoreBusinessHourModel?>> GetBusinessHoursAsync(int storeId)
        {
            string sql = "SELECT StoreId, DayOfWeek, OpeningTime, ClosingTime,IsClosed FROM StoreBusinessHours WHERE StoreId = @StoreId";

            return await _dbConnection.QueryAsync<StoreBusinessHourModel>(sql, new { StoreId = storeId });

        }

		//查詢
        public async Task<StoreBusinessHourModel?> GetBusinessHourByStoreAndDayAsync(int storeId, int dayOfWeek)
        {
            string sql = @"
SELECT TOP 1 StoreId, DayOfWeek, OpeningTime, ClosingTime, IsClosed
FROM StoreBusinessHours
WHERE StoreId = @StoreId AND DayOfWeek = @DayOfWeek";

            return await _dbConnection.QueryFirstOrDefaultAsync<StoreBusinessHourModel>(sql, new { StoreId = storeId, DayOfWeek = dayOfWeek });
        }

        //新增該星期營業時間資訊
        public async Task InsertBusinessHourAsync(StoreBusinessHourModel model)
        {
            string sql = @"
INSERT INTO StoreBusinessHours (StoreId, DayOfWeek, OpeningTime, ClosingTime, IsClosed)
VALUES (@StoreId, @DayOfWeek, @OpeningTime, @ClosingTime, @IsClosed)";

            await _dbConnection.ExecuteAsync(sql, model);
        }

		//更新該星期的營業時間資訊
        public async Task UpdateBusinessHourAsync(StoreBusinessHourModel model)
        {
            string sql = @"
UPDATE StoreBusinessHours
SET OpeningTime = @OpeningTime,
    ClosingTime = @ClosingTime,
    IsClosed = @IsClosed,
    UpdatedDate = GETDATE()
WHERE StoreId = @StoreId AND DayOfWeek = @DayOfWeek";

            await _dbConnection.ExecuteAsync(sql, model);
        }


        public async Task<IEnumerable<HolidayModel>> GetHolidaysAsync(int storeId)
        {
            string sql = @"
SELECT StoreId, HolidayDate, Description
FROM StoreHolidays
WHERE StoreId = @StoreId
ORDER BY HolidayDate";

            return await _dbConnection.QueryAsync<HolidayModel>(sql, new { StoreId = storeId });
        }


		//更新特殊假日
        public async Task UpdateHolidaysAsync(int storeId, IEnumerable<HolidayModel> models)
        {
            // 建議您改用連線字串（若您有 _connectionString 可取得）
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();

            try
            {
                Console.WriteLine("開始刪除所有特休假");

                // 如果您有刪除邏輯，請取消註解此行（並修正 SQL 拼字錯誤）
                string deleteSql = "DELETE FROM StoreHolidays WHERE StoreId = @StoreId";
                await connection.ExecuteAsync(deleteSql, new { StoreId = storeId }, transaction);

                Console.WriteLine("開始新增所有特休假");

                string insertSql = @"
INSERT INTO StoreHolidays (StoreId, HolidayDate, Description)
VALUES (@StoreId, @HolidayDate, @Description)";

                foreach (var model in models)
                {
                    await connection.ExecuteAsync(insertSql, model, transaction);
                }

                transaction.Commit();

                Console.WriteLine("成功新增所有特休假");
            }
            catch (Exception ex)
            {
                Console.WriteLine("特休假新增失敗：" + ex.Message);
                transaction.Rollback();
                throw;
            }
        }


    }

    public enum RegisterStatus
	{
		Success,
		UsernameExists,
		EmailExists,
		Failed
	}

}
