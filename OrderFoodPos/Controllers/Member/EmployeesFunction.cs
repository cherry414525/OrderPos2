using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using OrderFoodPos.Models.Member;
using OrderFoodPos.Services.Member;
using Microsoft.Azure.Functions.Worker.Http;
using System.Text.Json;


namespace OrderFoodPos.Controllers.Member
{
	public class EmployeesFunction
	{
		private readonly ILogger<EmployeesFunction> _logger;
		private readonly EmployeesService _service;

		public EmployeesFunction(ILogger<EmployeesFunction> logger, EmployeesService service)
		{
			_logger = logger;
			_service = service;
		}

		//創建員工
		[Function("CreateEmployee")]
		public async Task<IActionResult> CreateAsync(
	[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "employee/create")] HttpRequestData req)
		{
			try
			{
				string body = await req.ReadAsStringAsync();
				var emp = JsonSerializer.Deserialize<Employees>(body);

				if (emp == null || string.IsNullOrWhiteSpace(emp.Fullname))
					return new BadRequestObjectResult("資料不完整");

				bool success = await _service.CreateEmployeeAsync(emp);
				if (!success)
					return new StatusCodeResult(500);

				return new OkObjectResult("新增員工成功");
			}
			catch (Exception ex)
			{
				return new BadRequestObjectResult($"錯誤：{ex.Message}");
			}
		}

		//管理員獲取員工
		[Function("GetAccessibleEmployees")]
		public async Task<IActionResult> GetAccessibleEmployeesAsync(
	[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "employee/get")] HttpRequestData req)
		{
			try
			{
				string body = await req.ReadAsStringAsync();
				var input = JsonSerializer.Deserialize<QueryInput>(body);

				if (input == null || string.IsNullOrWhiteSpace(input.Username))
					return new BadRequestObjectResult("請提供 StoreId 與 Username");

				var result = await _service.QueryAccessibleEmployeesAsync(input.StoreId, input.Username);
				return new OkObjectResult(result);
			}
			catch (Exception ex)
			{
				return new BadRequestObjectResult($"查詢失敗：{ex.Message}");
			}
		}


		//更新員工
		[Function("UpdateEmployee")]
		public async Task<IActionResult> UpdateAsync(
	[HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "employee/update")] HttpRequestData req)
		{
			try
			{
				string body = await req.ReadAsStringAsync();
				var emp = JsonSerializer.Deserialize<Employees>(body);

				if (emp == null || string.IsNullOrWhiteSpace(emp.Username) || emp.StoreId <= 0)
					return new BadRequestObjectResult("缺少必要欄位");

				bool success = await _service.UpdateEmployeeAsync(emp);
				if (!success)
					return new StatusCodeResult(500);

				return new OkObjectResult("更新成功");
			}
			catch (Exception ex)
			{
				return new BadRequestObjectResult($"錯誤：{ex.Message}");
			}
		}


		//刪除員工
		[Function("DeleteEmployee")]
		public async Task<IActionResult> DeleteAsync(
	[HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "employee/delete")] HttpRequestData req)
		{
			string body = await req.ReadAsStringAsync();
			var data = JsonSerializer.Deserialize<Employees>(body);

			if (data == null || data.StoreId == 0 || string.IsNullOrWhiteSpace(data.Username))
				return new BadRequestObjectResult("請提供 StoreId 和 Username");

			try
			{
				bool result = await _service.DeleteEmployeeAsync(data.StoreId, data.Username);
				if (!result)
					return new NotFoundObjectResult("刪除失敗，找不到員工");

				return new OkObjectResult("員工刪除成功");
			}
			catch (Exception ex)
			{
				return new BadRequestObjectResult($"錯誤：{ex.Message}");
			}
		}


	}

	public class QueryInput
	{
		public int StoreId { get; set; }
		public string Username { get; set; } = string.Empty;
	}
}
