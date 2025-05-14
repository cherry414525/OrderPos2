using OrderFoodPos.Repositories.Member;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OrderFoodPos.Models.Member;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker;
using System.Net;

namespace OrderFoodPos.Services.Member
{
	public class StoreAccountService
	{
		private readonly StoreAccountRepository _memberRepo;
		private readonly EmployeesRepository _employeeRepo;

		public StoreAccountService(StoreAccountRepository memberRepo, EmployeesRepository employeeRepo)
		{
			_memberRepo = memberRepo;
			_employeeRepo = employeeRepo;
		}

		public async Task<bool> ValidateLoginAsync(string username, string password)
		{
			return await _memberRepo.IsLoginValidAsync(username, password);
		}

		public async Task<RegisterStatus> RegisterUserAsync(string username, string password, string email, string fullName)
		{
			return await _memberRepo.RegisterAsync(username, password, email, fullName);
		}

		public async Task<StoreAccountModel?> GetUserAsync(string username)
		{
			return await _memberRepo.GetUserByUsernameAsync(username); // 這個方法從資料庫撈該帳號
		}

		public async Task<LoginResult> LoginAsync(LoginRequest data)
		{
			// 嘗試從 StoreAccounts 登入
			var member = await _memberRepo.GetUserByUsernameAsync(data.Username);
			if (member != null)
			{
				if (member.Locked)
				{
					await _memberRepo.AddLoginHistoryAsync(data.Username, data.IP, data.DeviceId, data.Browser, data.OS, data.Screen, "Failed", "帳號已鎖定");
					return new LoginResult { Success = false, Message = "此帳號已被鎖定" };
				}

				bool valid = PasswordHelper.VerifyPassword(data.Password, member.PasswordHash, member.Salt);
				if (!valid)
				{
					await _memberRepo.AddLoginHistoryAsync(data.Username, data.IP, data.DeviceId, data.Browser, data.OS, data.Screen, "Failed", "密碼錯誤");
					return new LoginResult { Success = false, Message = "密碼錯誤" };
				}

				await _memberRepo.AddLoginHistoryAsync(data.Username, data.IP, data.DeviceId, data.Browser, data.OS, data.Screen, "Success");

				var store = await _memberRepo.GetStoreByAccountIdAsync(member.Id);

				var token = JwtHelper.GenerateToken(
					username: member.Username,
					fullName: member.FullName,
					email: member.Email,
					isAdmin: member.Admin ?? false, //根據資料庫中的 Role 欄位，不預設
					storeId: store?.Id ?? 0,
					storeName: store?.StoreName ?? ""
				);

				return new LoginResult { Success = true, Message = "登入成功", Token = token };
			}

			
			var emp = await _employeeRepo.FindEmployeeByUsernameAsync(data.Username);


			if (emp != null)
			{
				if (emp.Locked)
				{
					await _memberRepo.AddLoginHistoryAsync(data.Username, data.IP, data.DeviceId, data.Browser, data.OS, data.Screen, "Failed", "帳號已鎖定");
					return new LoginResult { Success = false, Message = "此帳號已被鎖定" };
				}

				bool valid = PasswordHelper.VerifyPassword(data.Password, emp.PasswordHash, emp.Salt);
				if (!valid)
				{
					await _memberRepo.AddLoginHistoryAsync(data.Username, data.IP, data.DeviceId, data.Browser, data.OS, data.Screen, "Failed", "密碼錯誤");
					return new LoginResult { Success = false, Message = "密碼錯誤" };
				}

				await _memberRepo.AddLoginHistoryAsync(data.Username, data.IP, data.DeviceId, data.Browser, data.OS, data.Screen, "Success");

				var token = JwtHelper.GenerateToken(
					username: emp.Username,
					fullName: emp.Fullname,
					email: emp.Email,
					isAdmin: emp.Admin, // ✅ 根據 StoreEmployees 的 Admin 欄位
					storeId: emp.StoreId,
					storeName: "" // 可選：你若想查 StoreName 再補上
				);

				return new LoginResult { Success = true, Message = "登入成功", Token = token };
			}

			// 兩者都找不到
			await _memberRepo.AddLoginHistoryAsync(data.Username, data.IP, data.DeviceId, data.Browser, data.OS, data.Screen, "Failed", "帳號不存在");
			return new LoginResult { Success = false, Message = "帳號不存在" };
		}


		public async Task<bool> UpdateMemberAsync(StoreAccountModel model)
		{
			// 你可以先檢查資料是否存在（可選）
			var existing = await _memberRepo.GetUserByUsernameAsync(model.Username);
			if (existing == null) return false;

			// 呼叫 Repository 更新
			return await _memberRepo.UpdateMemberAsync(model);
		}


		public async Task<LoginResult> CreateUserAsync(StoreAccountModel model)
		{
			var exists = await _memberRepo.GetUserByUsernameAsync(model.Username);
			if (exists != null)
			{
				return new LoginResult { Success = false, Message = "帳號已存在" };
			}

			// 自動產生 salt / hash
			var salt = PasswordHelper.GenerateSalt();
			var hash = PasswordHelper.HashPassword(model.PasswordHash, salt);

			model.PasswordHash = hash;
			model.Salt = salt;
			model.CreatedDate = DateTime.Now;
			model.UpdatedDate = DateTime.Now;

			var success = await _memberRepo.InsertMemberAsync(model);
			return new LoginResult { Success = success, Message = success ? "成功" : "建立失敗" };
		}

		//取得營業時間
        public async Task<IEnumerable<StoreBusinessHourModel?>>GetBusinessHoursAsync(int storeId)
        {

			return await _memberRepo.GetBusinessHoursAsync(storeId);
        }

        //更新每一天營業時間
        public async Task UpsertWeeklyBusinessHoursAsync(int storeId, List<StoreBusinessHourModel> businessHours)
        {
            foreach (var businessHour in businessHours)
            {
                if (businessHour == null || businessHour.DayOfWeek < 1 || businessHour.DayOfWeek > 7 || businessHour.StoreId != storeId)
                    continue;

				//查詢該星期是否存在
                var existing = await _memberRepo.GetBusinessHourByStoreAndDayAsync(storeId, businessHour.DayOfWeek);

				//如果存在更新資料，如果沒有資料則新增
                if (existing == null)
                {
                    await _memberRepo.InsertBusinessHourAsync(businessHour);
                }
                else
                {
                    await _memberRepo.UpdateBusinessHourAsync(businessHour);
                }
            }
        }


		//取得特殊假日
        public async Task<IEnumerable<HolidayModel>> GetHolidaysAsync(int storeId)
        {
            return await _memberRepo.GetHolidaysAsync(storeId);
        }

		//更新特殊假日
        public async Task UpdateHolidaysAsync(int storeId, IEnumerable<HolidayModel> models)
        {
            await _memberRepo.UpdateHolidaysAsync(storeId, models);
        }



    }
}
