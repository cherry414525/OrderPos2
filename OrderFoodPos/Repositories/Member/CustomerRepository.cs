using OrderFoodPos.Models.Member;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderFoodPos.Repositories.Member
{

	public class CustomerRepository : BaseRepository<StoreCustomer>
	{
		public CustomerRepository(IDbConnection dbConnection) : base(dbConnection) { }

		//獲取電話號碼
		public async Task<StoreCustomer?> GetByPhoneAsync(string phone)
		{
			var sql = "SELECT * FROM StoreCustomer WHERE PhoneNumber = @PhoneNumber";
			return await GetAsync(sql, new { PhoneNumber = phone });
		}


		//創建帳號
		public async Task<int> CreateAsync(StoreCustomer customer)
		{
			var sql = @"
        INSERT INTO StoreCustomer (
             Name, PhoneNumber, LineId, Email, Locked, CreatedDate, UpdatedDate
        )
        VALUES (
             @Name, @PhoneNumber, @LineId, @Email, @Locked, @CreatedDate, @UpdatedDate
        )";
			return await AddAsync(sql, customer);
		}


		//查詢是否有對應的LINE帳號
		public async Task<StoreCustomer?> GetByLineIdAsync(string lineId)
		{
			var sql = "SELECT * FROM StoreCustomer WHERE LineId = @LineId";
			return await GetAsync(sql, new { LineId = lineId });
		}

		//創建登入Log
		public async Task<int> CreateLogAsync(StoreCustomerLoginLog log)
		{
			var sql = @"INSERT INTO StoreCustomerLoginLog 
                    (CustomerId, LoginType, LoginTime, IpAddress, UserAgent, Success, Message) 
                    VALUES 
                    (@CustomerId, @LoginType, @LoginTime, @IpAddress, @UserAgent, @Success, @Message)";
			return await AddAsync(sql, log);
		}

	}

}
