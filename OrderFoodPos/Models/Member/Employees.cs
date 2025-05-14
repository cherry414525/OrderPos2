using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderFoodPos.Models.Member
{
	public class Employees
	{
		public int? Id { get; set; }
		public int StoreId { get; set; }
		public string Fullname { get; set; }
		public string Username { get; set; }
		public string PasswordHash { get; set; }
		public string? Salt { get; set; }
		public string Email { get; set; }
		public string Phone { get; set; }
		public bool Admin { get; set; }
		public bool Locked { get; set; }
		public string? SalaryType { get; set; }      // 月薪 / 時薪
		public decimal Salary { get; set; }
		public string? Creator { get; set; }
		public DateTime CreatedDate { get; set; }
		public DateTime? UpdatedDate { get; set; }
	}


	public class StoreEmployeeAttendance
	{
		public int Id { get; set; }
		public int EmployeeId { get; set; }
		public DateTime WorkDate { get; set; }
		public DateTime? ClockIn { get; set; }
		public DateTime? ClockOut { get; set; }
		public DateTime? OvertimeStart { get; set; }
		public DateTime? OvertimeEnd { get; set; }
		public DateTime CreatedDate { get; set; }
		public DateTime? UpdateDate { get; set; }
	}
}
