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
			PersonnelRepository personnelRepo) // <== �[�W�o��
		{
			_logger = logger;
			_employeeRepo = employeeRepo;
			_personnelRepo = personnelRepo; // <== �x�s�i���
		}

		// ���d�n�J�t��
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
				await response.WriteAsJsonAsync(new { message = "�п�J�b���P�K�X" });
				return response;
			}

			var emp = await _employeeRepo.FindEmployeeByUsernameAsync(data.Username);
			if (emp == null)
			{
				response.StatusCode = HttpStatusCode.Unauthorized;
				await response.WriteAsJsonAsync(new { message = "�b�����s�b" });
				return response;
			}

			if (emp.Locked)
			{
				response.StatusCode = HttpStatusCode.Forbidden;
				await response.WriteAsJsonAsync(new { message = "�b���w�Q��w" });
				return response;
			}

			bool isValid = PasswordHelper.VerifyPassword(data.Password, emp.PasswordHash, emp.Salt);
			if (!isValid)
			{
				response.StatusCode = HttpStatusCode.Unauthorized;
				await response.WriteAsJsonAsync(new { message = "�K�X���~" });
				return response;
			}

			if (string.IsNullOrWhiteSpace(data.Type))
			{
				response.StatusCode = HttpStatusCode.BadRequest;
				await response.WriteAsJsonAsync(new { message = "�д��ѥ��d���� (Type)" });
				return response;
			}

			var attendance = await _personnelRepo.GetTodayAttendanceAsync(emp.Id.Value);

			switch (data.Type)
			{
				case "ClockIn":
					if (attendance != null && attendance.ClockIn != null)
					{
						response.StatusCode = HttpStatusCode.Conflict;
						await response.WriteAsJsonAsync(new { message = "���Ѥw�g���L�W�Z�d" });
					}
					else if (attendance == null)
					{
						await _personnelRepo.InsertClockInAsync(emp.Id.Value);
						await response.WriteAsJsonAsync(new { message = "�W�Z���d���\" });
					}
					else
					{
						await _personnelRepo.UpdateClockInAsync(attendance.Id);
						await response.WriteAsJsonAsync(new { message = "�ɥ��W�Z�d���\" });
					}
					break;

				case "ClockOut":
					if (attendance == null || attendance.ClockIn == null)
					{
						response.StatusCode = HttpStatusCode.BadRequest;
						await response.WriteAsJsonAsync(new { message = "�|���W�Z�A�L�k�U�Z���d" });
					}
					else if (attendance.ClockOut != null)
					{
						response.StatusCode = HttpStatusCode.Conflict;
						await response.WriteAsJsonAsync(new { message = "���Ѥw�g���L�U�Z�d" });
					}
					else
					{
						await _personnelRepo.UpdateClockOutAsync(attendance.Id);
						await response.WriteAsJsonAsync(new { message = "�U�Z���d���\" });
					}
					break;

				case "OvertimeStart":
					if (attendance == null || attendance.ClockOut == null)
					{
						response.StatusCode = HttpStatusCode.BadRequest;
						await response.WriteAsJsonAsync(new { message = "�|���U�Z�A����[�Z" });
					}
					else if (attendance.OvertimeStart != null)
					{
						response.StatusCode = HttpStatusCode.Conflict;
						await response.WriteAsJsonAsync(new { message = "�[�Z�}�l�w���d" });
					}
					else
					{
						await _personnelRepo.UpdateOvertimeStartAsync(attendance.Id);
						await response.WriteAsJsonAsync(new { message = "�[�Z�}�l���d���\" });
					}
					break;

				case "OvertimeEnd":
					if (attendance == null || attendance.OvertimeStart == null)
					{
						response.StatusCode = HttpStatusCode.BadRequest;
						await response.WriteAsJsonAsync(new { message = "�|���}�l�[�Z�A�L�k����" });
					}
					else if (attendance.OvertimeEnd != null)
					{
						response.StatusCode = HttpStatusCode.Conflict;
						await response.WriteAsJsonAsync(new { message = "�[�Z�����w���d" });
					}
					else
					{
						await _personnelRepo.UpdateOvertimeEndAsync(attendance.Id);
						await response.WriteAsJsonAsync(new { message = "�[�Z�������d���\" });
					}
					break;

				default:
					response.StatusCode = HttpStatusCode.BadRequest;
					await response.WriteAsJsonAsync(new { message = "���������d���� (Type)" });
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




