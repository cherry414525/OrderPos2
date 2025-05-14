using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using OrderFoodPos.Services.Menu;
using System.Text.Json;
using System.Net;
using OrderFoodPos.Models.Menu;
using Microsoft.Azure.Functions.Worker.Http;
using System.Text.Json.Serialization;


namespace OrderFoodPos.Controllers.Menu;

public class ItemFunction
{
    private readonly ILogger<ItemFunction> _logger;
	private readonly ItemService _service;

	public ItemFunction(ILogger<ItemFunction> logger, ItemService service)
	{
		_logger = logger;
		_service = service;
	}

	[Function("GetMenuItemById")]
	public async Task<IActionResult> GetMenuItemById(
	   [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "menuitems/{id:int}")] HttpRequestData req,
	   int id)
	{
		var result = await _service.GetByIdAsync(id); // 查詢資料
		if (result == null)
			return new NotFoundObjectResult($"找不到餐點 id: {id}"); // 若查無資料回傳 404

		return new OkObjectResult(result); // 成功回傳資料
	}


	[Function("GetMenuItemsByStore")]
	public async Task<IActionResult> GetMenuItemsByStore(
	[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "menuitems/get")] HttpRequestData req)
	{
		var body = await req.ReadAsStringAsync(); // 讀取 JSON
		var data = JsonSerializer.Deserialize<Dictionary<string, int>>(body);

		if (!data.TryGetValue("StoreId", out int StoreId) || StoreId <= 0)
			return new BadRequestObjectResult("storeId 無效");

		var result = await _service.GetAllByStoreIdAsync(StoreId); // 呼叫 Service 層取得資料
		return new OkObjectResult(result); // 回傳餐點列表
	}




	/// <summary>
	/// 新增餐點 API（使用已上傳的圖片 URL）
	/// </summary>
	[Function("CreateMenuItem")]
	public async Task<HttpResponseData> CreateMenuItem(
		[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "Menu/items")] HttpRequestData req,
		FunctionContext executionContext)
	{
		var logger = executionContext.GetLogger("CreateMenuItem");
		logger.LogInformation("收到新增餐點請求");

		try
		{
			// 設定 JSON 選項以忽略大小寫
			var jsonOptions = new JsonSerializerOptions
			{
				PropertyNameCaseInsensitive = true,
				DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
			};

			// 從請求中反序列化 MenuItem 物件
			var model = await JsonSerializer.DeserializeAsync<MenuItem>(req.Body, jsonOptions);

			// 驗證基本必填欄位
			if (model == null || string.IsNullOrWhiteSpace(model.Name))
			{
				var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
				await badResponse.WriteStringAsync("餐點名稱為必填欄位");
				return badResponse;
			}

			// 如果沒有指定 Sequence，設置一個預設值
			if (model.Sequence <= 0)
			{
				model.Sequence = 999; // 預設放在最後
			}

			// 呼叫 Service 層新增餐點
			var newId = await _service.AddAsync(model);

			// 建立成功回應
			var response = req.CreateResponse(HttpStatusCode.Created);
			var jsonString = JsonSerializer.Serialize(new
			{
				id = newId,
				menuItem = model
			}, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

			response.Headers.Add("Content-Type", "application/json; charset=utf-8");
			await response.WriteStringAsync(jsonString);
			return response;
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "新增餐點時發生錯誤");

			// 建立錯誤回應
			var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
			await errorResponse.WriteStringAsync($"處理請求時發生錯誤: {ex.Message}");
			return errorResponse;
		}
	}


	/// <summary>
	/// 獲取上傳圖片用的 SAS 令牌
	/// </summary>
	[Function("GetUploadSasToken")]
	public async Task<HttpResponseData> GetUploadSasToken(
		[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "Menu/getUploadSasToken")] HttpRequestData req,
		FunctionContext executionContext)
	{
		var logger = executionContext.GetLogger("GetUploadSasToken");
		logger.LogInformation("收到獲取 SAS 令牌請求");

		try
		{
			// 從請求中獲取商店 ID
			var requestBody = await req.ReadAsStringAsync();
			var data = JsonSerializer.Deserialize<Dictionary<string, int>>(requestBody);

			if (!data.TryGetValue("StoreId", out int storeId) || storeId <= 0)
			{
				var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
				await badResponse.WriteStringAsync("無效的商店 ID");
				return badResponse;
			}

			// 生成 SAS 令牌
			var sasInfo = await _service.GenerateUploadSasToken(storeId);

			// 返回 SAS 信息
			var response = req.CreateResponse(HttpStatusCode.OK);
			await response.WriteAsJsonAsync(sasInfo);
			return response;
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "生成 SAS 令牌時發生錯誤");
			var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
			await errorResponse.WriteStringAsync($"處理請求時發生錯誤: {ex.Message}");
			return errorResponse;
		}
	}


	[Function("ImportMenuData")]
	public async Task<HttpResponseData> ImportMenuData(
	[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "menu/import")] HttpRequestData req)
	{
		_logger.LogInformation("收到菜單匯入請求");

		var body = await req.ReadAsStringAsync();
		var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
		var request = JsonSerializer.Deserialize<ImportMenuRequest>(body, options);

		if (request == null || request.StoreId <= 0)
		{
			var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
			await badResponse.WriteStringAsync("缺少或無效的 StoreId");
			return badResponse;
		}

		try
		{
			//如果是覆蓋模式，先清空資料
			if (request.Overwrite)
			{
				await _service.DeleteAllCategoriesAndItemsByStoreAsync(request.StoreId);
			}

			//建立分類，並記住新增的 Id 對應分類名稱
			var createdCategories = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase); // 不區分大小寫

			foreach (var category in request.Categories)
			{
				if (string.IsNullOrWhiteSpace(category.Name))
				{
					_logger.LogWarning("匯入時發現空白類別名稱，已跳過");
					continue;
				}

				category.StoreId = request.StoreId;
				var id = await _service.CreateAsync(category);
				createdCategories[category.Name.Trim()] = id;
			}

			//建立餐點資料
			foreach (var itemDto in request.MenuItems)
			{
				if (string.IsNullOrWhiteSpace(itemDto.CategoryName))
				{
					_logger.LogWarning($"餐點 {itemDto.Name} 沒有提供 CategoryName，已跳過");
					continue;
				}

				if (!createdCategories.TryGetValue(itemDto.CategoryName.Trim(), out int categoryId))
				{
					_logger.LogWarning($"找不到對應類別: {itemDto.CategoryName}");
					continue;
				}

				var item = new MenuItem
				{
					Name = itemDto.Name,
					Price = itemDto.Price,
					StoreId = request.StoreId,
					CategoryId = categoryId,
					Stock = itemDto.Stock,
					Sequence = itemDto.Sequence,
					Meter = itemDto.Meter
				};

				await _service.CreateMenuItemAsync(item);
			}

			var response = req.CreateResponse(HttpStatusCode.OK);
			await response.WriteStringAsync("匯入成功");
			return response;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "菜單匯入時發生錯誤");
			var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
			await errorResponse.WriteStringAsync("處理菜單匯入失敗：" + ex.Message);
			return errorResponse;
		}
	}


	[Function("DeleteMenuItem")]
	public async Task<HttpResponseData> DeleteMenuItem(
	[HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "menuitems/{id:int}")] HttpRequestData req,
	int id)
	{
		_logger.LogInformation($"收到刪除餐點的請求，ID: {id}");

		try
		{
			await _service.DeleteMenuItemAsync(id); // 呼叫 Service 刪除方法

			var response = req.CreateResponse(HttpStatusCode.OK);
			await response.WriteStringAsync("刪除成功");
			return response;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, $"刪除餐點失敗，ID: {id}");
			var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
			await errorResponse.WriteStringAsync($"刪除失敗: {ex.Message}");
			return errorResponse;
		}
	}

}