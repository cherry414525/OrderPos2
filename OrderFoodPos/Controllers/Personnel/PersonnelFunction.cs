using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using OrderFoodPos.Repositories.Member;
using OrderFoodPos.Services;
using Microsoft.Azure.Functions.Worker.Http;
using System.Text.Json;
using System.Net;
using OrderFoodPos.Repositories.Personnel;


namespace OrderFoodPos.Controllers.Personnel
{
    public class PersonnelFunction
    {
		private readonly ILogger<PersonnelFunction> _logger;
		private readonly EmployeesRepository _employeeRepo;
		private readonly PersonnelRepository _personnelRepo;

		public PersonnelFunction(
			ILogger<PersonnelFunction> logger,
			EmployeesRepository employeeRepo,
			PersonnelRepository personnelRepo) // <== 加上這個
		{
			_logger = logger;
			_employeeRepo = employeeRepo;
			_personnelRepo = personnelRepo; // <== 儲存進欄位
		}

		// 打卡登入系統
		[Function("PersonnelLogin")]
		public async Task<HttpResponseData> Run(
			[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "personnel/login")] HttpRequestData req)
		{
			var response = req.CreateResponse();
			string body = await new StreamReader(req.Body).ReadToEndAsync();
			var data = JsonSerializer.Deserialize<LoginRequest>(body);

			if (data == null || string.IsNullOrWhiteSpace(data.Username) || string.IsNullOrWhiteSpace(data.Password))
			{
				response.StatusCode = HttpStatusCode.BadRequest;
				await response.WriteAsJsonAsync(new { message = "請輸入帳號與密碼" });
				return response;
			}

			var emp = await _employeeRepo.FindEmployeeByUsernameAsync(data.Username);
			if (emp == null)
			{
				response.StatusCode = HttpStatusCode.Unauthorized;
				await response.WriteAsJsonAsync(new { message = "帳號不存在" });
				return response;
			}

			if (emp.Locked)
			{
				response.StatusCode = HttpStatusCode.Forbidden;
				await response.WriteAsJsonAsync(new { message = "帳號已被鎖定" });
				return response;
			}

			bool isValid = PasswordHelper.VerifyPassword(data.Password, emp.PasswordHash, emp.Salt);
			if (!isValid)
			{
				response.StatusCode = HttpStatusCode.Unauthorized;
				await response.WriteAsJsonAsync(new { message = "密碼錯誤" });
				return response;
			}

			if (string.IsNullOrWhiteSpace(data.Type))
			{
				response.StatusCode = HttpStatusCode.BadRequest;
				await response.WriteAsJsonAsync(new { message = "請提供打卡類型 (Type)" });
				return response;
			}

			var attendance = await _personnelRepo.GetTodayAttendanceAsync(emp.Id.Value);

			switch (data.Type)
			{
				case "ClockIn":
					if (attendance != null && attendance.ClockIn != null)
					{
						response.StatusCode = HttpStatusCode.Conflict;
						await response.WriteAsJsonAsync(new { message = "今天已經打過上班卡" });
					}
					else if (attendance == null)
					{
						await _personnelRepo.InsertClockInAsync(emp.Id.Value);
						await response.WriteAsJsonAsync(new { message = "上班打卡成功" });
					}
					else
					{
						await _personnelRepo.UpdateClockInAsync(attendance.Id);
						await response.WriteAsJsonAsync(new { message = "補打上班卡成功" });
					}
					break;

				case "ClockOut":
					if (attendance == null || attendance.ClockIn == null)
					{
						response.StatusCode = HttpStatusCode.BadRequest;
						await response.WriteAsJsonAsync(new { message = "尚未上班，無法下班打卡" });
					}
					else if (attendance.ClockOut != null)
					{
						response.StatusCode = HttpStatusCode.Conflict;
						await response.WriteAsJsonAsync(new { message = "今天已經打過下班卡" });
					}
					else
					{
						await _personnelRepo.UpdateClockOutAsync(attendance.Id);
						await response.WriteAsJsonAsync(new { message = "下班打卡成功" });
					}
					break;

				case "OvertimeStart":
					if (attendance == null || attendance.ClockOut == null)
					{
						response.StatusCode = HttpStatusCode.BadRequest;
						await response.WriteAsJsonAsync(new { message = "尚未下班，不能加班" });
					}
					else if (attendance.OvertimeStart != null)
					{
						response.StatusCode = HttpStatusCode.Conflict;
						await response.WriteAsJsonAsync(new { message = "加班開始已打卡" });
					}
					else
					{
						await _personnelRepo.UpdateOvertimeStartAsync(attendance.Id);
						await response.WriteAsJsonAsync(new { message = "加班開始打卡成功" });
					}
					break;

				case "OvertimeEnd":
					if (attendance == null || attendance.OvertimeStart == null)
					{
						response.StatusCode = HttpStatusCode.BadRequest;
						await response.WriteAsJsonAsync(new { message = "尚未開始加班，無法結束" });
					}
					else if (attendance.OvertimeEnd != null)
					{
						response.StatusCode = HttpStatusCode.Conflict;
						await response.WriteAsJsonAsync(new { message = "加班結束已打卡" });
					}
					else
					{
						await _personnelRepo.UpdateOvertimeEndAsync(attendance.Id);
						await response.WriteAsJsonAsync(new { message = "加班結束打卡成功" });
					}
					break;

				default:
					response.StatusCode = HttpStatusCode.BadRequest;
					await response.WriteAsJsonAsync(new { message = "未知的打卡類型 (Type)" });
					break;
			}

			return response;
		}



	}

	public class LoginRequest
	{
		public string Username { get; set; }
		public string Password { get; set; }
		public string Type { get; set; }
	}

}




