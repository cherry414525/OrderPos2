using Dapper;
using OrderFoodPos.Models.Member;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderFoodPos.Repositories.Member
{
	public class EmployeesRepository : BaseRepository<Employees>
	{
		public EmployeesRepository(IDbConnection dbConnection) : base(dbConnection) { }

		//檢查帳號是否存在
		public async Task<bool> IsUserNameExistsAsync(int storeId, string userName)
		{
			string sql = @"
SELECT COUNT(1)
FROM StoreEmployees
WHERE StoreId = @StoreId AND Username = @Username;

SELECT COUNT(1)
FROM StoreAccounts
WHERE Username = @Username;
";

			using var multi = await _dbConnection.QueryMultipleAsync(sql, new { StoreId = storeId, UserName = userName });
			int empCount = await multi.ReadFirstAsync<int>();
			int accCount = await multi.ReadFirstAsync<int>();

			return empCount > 0 || accCount > 0;
		}

		//檢查帳號是否存在
		public async Task<Employees?> FindEmployeeByUsernameAsync(string username)
		{
			string sql = "SELECT * FROM StoreEmployees WHERE Username = @Username";
			return await GetAsync(sql, new { Username = username });
		}




		//檢查商店是否存在
		public async Task<bool> IsStoreExistsAsync(int storeId)
		{
			string sql = "SELECT COUNT(1) FROM Stores WHERE Id = @Id";
			int count = await ExecuteScalarAsync<int>(sql, new { Id = storeId });
			return count > 0;
		}



		//新增員工
		public async Task<bool> AddEmployeeAsync(Employees emp)
		{
			string sql = @"
INSERT INTO StoreEmployees
(StoreId, Fullname, Email, Phone, Username, PasswordHash, Salt, Admin, Locked, SalaryType, Salary, Creator, CreatedDate, UpdatedDate)
VALUES
(@StoreId, @Fullname, @Email, @Phone, @Username, @PasswordHash, @Salt, @Admin, @Locked, @SalaryType, @Salary, @Creator, GETDATE(), NULL)";


			int rows = await AddAsync(sql, emp);
			return rows > 0;
		}


		//查詢員工
		public async Task<IEnumerable<Employees>> GetAccessibleEmployeesAsync(int storeId, string username)
		{
			// 查是否為商店擁有者帳號
			string sqlAccount = @"
SELECT COUNT(1)
FROM StoreAccounts sa
JOIN Stores s ON sa.Id = s.StoreAccountsId
WHERE s.Id = @StoreId AND sa.Username = @Username";

			int accountCount = await ExecuteScalarAsync<int>(sqlAccount, new { StoreId = storeId, Username = username });

			if (accountCount > 0)
			{
				// 是商店帳號，回傳所有員工
				string sqlAllEmp = "SELECT * FROM StoreEmployees WHERE StoreId = @StoreId";
				return await GetAllAsync(sqlAllEmp, new { StoreId = storeId });
			}

			// 查 StoreEmployee 並確認是否為 Admin
			string sqlEmp = "SELECT * FROM StoreEmployees WHERE StoreId = @StoreId AND Username = @Username";
			var employee = await GetAsync(sqlEmp, new { StoreId = storeId, Username = username });

			if (employee != null && employee.Admin)
			{
				// 是 Admin 員工，回傳非 Admin 員工
				string sqlSubEmp = "SELECT * FROM StoreEmployees WHERE StoreId = @StoreId AND Admin = 0";
				return await GetAllAsync(sqlSubEmp, new { StoreId = storeId });
			}

			throw new Exception("無權限查詢員工或帳號不存在");
		}

		//更新員工
		public async Task<bool> UpdateEmployeeAsync(Employees emp)
		{
			string sql = @"
UPDATE StoreEmployees
SET 
    Fullname = @Fullname,
	Phone = @Phone,
	Admin = @Admin,
	SalaryType = @SalaryType,
	Salary = @Salary,
	Locked = @Locked,
	Creator = @Creator,
	UpdatedDate = @UpdatedDate
WHERE StoreId = @StoreId AND Username = @Username";

			int affectedRows = await _dbConnection.ExecuteAsync(sql, emp);
			return affectedRows > 0;
		}

		//查詢員工是否符合資料
		public async Task<Employees?> FindEmployeeAsync(int storeId, string username)
		{
			string sql = "SELECT * FROM StoreEmployees WHERE StoreId = @StoreId AND Username = @Username";
			return await GetAsync(sql, new { StoreId = storeId, Username = username });
		}

		//刪除員工
		public async Task<bool> DeleteEmployeeAsync(int storeId, string username)
		{
			string sql = @"
DELETE FROM StoreEmployees
WHERE StoreId = @StoreId AND Username = @Username";

			int affectedRows = await _dbConnection.ExecuteAsync(sql, new { StoreId = storeId, Username = username });
			return affectedRows > 0;
		}


	}
}
