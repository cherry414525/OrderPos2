using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderFoodPos.Models.Member
{
	public class StoreAccountModel
	{
		public int Id { get; set; }
		public string Username { get; set; }       // 使用者帳號
		public string PasswordHash { get; set; }   // 密碼雜湊值
		public string Email { get; set; }
		public string? Salt { get; set; }           // 密碼加鹽
		public string FullName { get; set; }       // 使用者姓名
		public string? Phone { get; set; }
		public bool Locked { get; set; }            // 是否鎖定
		public bool? Admin { get; set; }            // 是否為管理員
		public DateTime? CreatedDate { get; set; }
		public DateTime? UpdatedDate { get; set; }
		public DateTime? PasswordChangeDate { get; set; }
	}

	
		
	


	public class RegisterRequest
	{
		public string Username { get; set; }      // 使用者帳號
		public string Password { get; set; }      // 使用者密碼
		public string Email { get; set; }         // 使用者 Email
		public string FullName { get; set; }      // 真實姓名
	}



	public class LoginRequest
	{
		public string Username { get; set; }
		public string Password { get; set; }
		public string IP { get; set; }
		public string DeviceId { get; set; }
		public string Browser { get; set; }
		public string OS { get; set; }
		public string Screen { get; set; }
	}

	public class LoginResponse
	{
		public string Token { get; set; }
		public string RefreshToken { get; set; }
		public DateTime Expiry { get; set; }
	}

	

	public class RegisterResponse
	{
		public string Message { get; set; }
	}

	public class LoginResult
	{
		public bool Success { get; set; }
		public string Message { get; set; }
		public string Token { get; set; }
	}


	public class StoreModel
	{
		public int Id { get; set; }
		public int StoreAccountsId { get; set; }
		public string StoreName { get; set; }
	}


    public class StoreBusinessHourModel
    {
        public int StoreId { get; set; }
        public int DayOfWeek { get; set; }  // 1=星期一, 2=星期二, ..., 7=星期日
        public TimeSpan OpeningTime { get; set; }
        public TimeSpan ClosingTime { get; set; }

		public bool IsClosed {get; set; }
}

    public class HolidayModel
    {
        public int StoreId { get; set; }
        public DateTime HolidayDate { get; set; }
        public string Description { get; set; } = string.Empty;
    }


}
