using OrderFoodPos.Models.Member;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderFoodPos.Repositories.Personnel
{
	public class PersonnelRepository : BaseRepository<StoreEmployeeAttendance>
	{
		public PersonnelRepository(IDbConnection dbConnection) : base(dbConnection) { }

		public async Task<StoreEmployeeAttendance?> GetTodayAttendanceAsync(int empId)
		{
			string sql = @"
SELECT * FROM StoreEmployeeAttendance 
WHERE EmployeeId = @EmployeeId AND WorkDate = CAST(GETDATE() AS DATE)";
			return await GetAsync(sql, new { EmployeeId = empId });
		}

		public async Task<bool> InsertClockInAsync(int empId)
		{
			string sql = @"
INSERT INTO StoreEmployeeAttendance
(EmployeeId, WorkDate, ClockIn, CreatedDate)
VALUES (@EmployeeId, CAST(GETDATE() AS DATE), GETDATE(), GETDATE())";
			int affected = await ExecuteAsync(sql, new { EmployeeId = empId });
			return affected > 0;
		}

		public async Task<bool> UpdateClockOutAsync(int id)
		{
			string sql = @"
UPDATE StoreEmployeeAttendance
SET ClockOut = GETDATE(), UpdateDate = GETDATE()
WHERE Id = @Id";
			return await ExecuteAsync(sql, new { Id = id }) > 0;
		}

		public async Task<bool> UpdateOvertimeStartAsync(int id)
		{
			string sql = @"
UPDATE StoreEmployeeAttendance
SET OvertimeStart = GETDATE(), UpdateDate = GETDATE()
WHERE Id = @Id";
			return await ExecuteAsync(sql, new { Id = id }) > 0;
		}

		public async Task<bool> UpdateOvertimeEndAsync(int id)
		{
			string sql = @"
UPDATE StoreEmployeeAttendance
SET OvertimeEnd = GETDATE(), UpdateDate = GETDATE()
WHERE Id = @Id";
			return await ExecuteAsync(sql, new { Id = id }) > 0;
		}

		public async Task<bool> UpdateClockInAsync(int id)
		{
			string sql = @"
UPDATE StoreEmployeeAttendance
SET ClockIn = GETDATE(), UpdateDate = GETDATE()
WHERE Id = @Id AND ClockIn IS NULL"; // 避免覆蓋已存在資料

			return await ExecuteAsync(sql, new { Id = id }) > 0;
		}

	}
}
