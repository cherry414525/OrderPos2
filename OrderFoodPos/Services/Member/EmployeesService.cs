using OrderFoodPos.Models.Member;
using OrderFoodPos.Repositories.Member;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderFoodPos.Services.Member
{
	public class EmployeesService
	{
		private readonly EmployeesRepository _repo;

		public EmployeesService(EmployeesRepository repo)
		{
			_repo = repo;
		}

		//新增員工
		public async Task<bool> CreateEmployeeAsync(Employees emp)
		{
			if (string.IsNullOrWhiteSpace(emp.PasswordHash))
				return false;

			// 驗證商店是否存在
			if (!await _repo.IsStoreExistsAsync(emp.StoreId))
				throw new Exception("找不到對應的商店");

			// 驗證帳號是否在同一商店下已存在
			if (await _repo.IsUserNameExistsAsync(emp.StoreId, emp.Username))
				throw new Exception("該商店已存在相同帳號");

			// 密碼加密
			string salt = PasswordHelper.GenerateSalt();
			string hashedPassword = PasswordHelper.HashPassword(emp.PasswordHash, salt);

			emp.Salt = salt;
			emp.PasswordHash = hashedPassword;

			return await _repo.AddEmployeeAsync(emp);
		}


		//查詢員工
		public async Task<IEnumerable<Employees>> QueryAccessibleEmployeesAsync(int storeId, string username)
		{
			return await _repo.GetAccessibleEmployeesAsync(storeId, username);
		}

		//更新員工
		public async Task<bool> UpdateEmployeeAsync(Employees emp)
		{
			// 驗證 Store 是否存在
			if (!await _repo.IsStoreExistsAsync(emp.StoreId))
				throw new Exception("找不到對應的商店");

			// 驗證員工是否存在
			var existing = await _repo.FindEmployeeAsync(emp.StoreId, emp.Username);
			if (existing == null)
				throw new Exception("找不到該員工");

			// 設定修改時間
			emp.UpdatedDate = DateTime.Now;

			return await _repo.UpdateEmployeeAsync(emp);
		}


		//刪除員工
		public async Task<bool> DeleteEmployeeAsync(int storeId, string username)
		{
			// 驗證商店是否存在
			if (!await _repo.IsStoreExistsAsync(storeId))
				throw new Exception("找不到對應的商店");

			// 驗證員工是否存在
			var existing = await _repo.FindEmployeeByUsernameAsync(username);
			if (existing == null || existing.StoreId != storeId)
				throw new Exception("找不到該員工");

			// 執行刪除
			return await _repo.DeleteEmployeeAsync(storeId, username);
		}


	}
}
