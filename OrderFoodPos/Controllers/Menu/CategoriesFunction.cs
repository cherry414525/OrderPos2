using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using OrderFoodPos.Models.Menu;
using OrderFoodPos.Services.Menu;
using Microsoft.Azure.Functions.Worker.Http;
using System.Text.Json;
using System.Net;


namespace OrderFoodPos.Controllers.Menu;

public class CategoriesFunction
{
    private readonly ILogger<CategoriesFunction> _logger;
	private readonly CategoriesService _service;

	public CategoriesFunction(ILogger<CategoriesFunction> logger, CategoriesService service)
	{
		_logger = logger;
		_service = service;
	}

	[Function("GetMenuCategories")]
	public async Task<HttpResponseData> Run(
		[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "categories/get")] HttpRequestData req)
	{
		_logger.LogInformation("收到取得分類的請求");

		var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
		var request = JsonSerializer.Deserialize<GetCategoryRequest>(requestBody);

		if (request == null || request.StoreId <= 0)
		{
			var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
			await badResponse.WriteStringAsync("缺少或無效的 StoreId");
			return badResponse;
		}

		var result = await _service.GetCategoriesByStoreIdAsync(request.StoreId);
		var response = req.CreateResponse(HttpStatusCode.OK);
		await response.WriteAsJsonAsync(result);
		return response;
	}

	[Function("CreateCategory")]
	public async Task<IActionResult> CreateCategory(
		[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "categories")] HttpRequestData req)
	{
		var body = await req.ReadAsStringAsync();
		var category = JsonSerializer.Deserialize<MenuCategory>(body);
		if (category == null)
			return new BadRequestObjectResult("Invalid category format");

		var id = await _service.CreateAsync(category);
		return new OkObjectResult(new { id });
	}

	[Function("UpdateCategory")]
	public async Task<IActionResult> UpdateCategory(
	[HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "categories/{id:int}")] HttpRequestData req,
	int id)
	{
		var body = await req.ReadAsStringAsync();
		var category = JsonSerializer.Deserialize<MenuCategory>(body);

		if (category == null || string.IsNullOrWhiteSpace(category.Name))
			return new BadRequestObjectResult("錯誤的類別資料");

		await _service.UpdateCategoryAsync(id, category.Name, category.StoreId, category.Sequence ?? 0);
		return new OkResult();
	}


	[Function("DeleteCategory")]
	public async Task<IActionResult> DeleteCategory(
		[HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "categories/{id:int}")] HttpRequestData req,
		int id)
	{
		await _service.DeleteAsync(id);
		return new OkResult();
	}
}
