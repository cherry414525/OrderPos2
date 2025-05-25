using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using OrderFoodPos.Models.Store;
using OrderFoodPos.Services.Store;
using System.Net;

namespace OrderFoodPos.Controllers.Store;

public class TasteFunction
{
	private readonly ILogger<TasteFunction> _logger;
	private readonly TasteService _service;

	public TasteFunction(ILogger<TasteFunction> logger, TasteService service)
	{
		_logger = logger;
		_service = service;
	}

	//獲得店家口味設定
	[Function("GetAddGroups")]
	public async Task<HttpResponseData> Run(
			[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "store/{storeId}/add-groups")] HttpRequestData req,
			int storeId)
	{
		var response = req.CreateResponse(HttpStatusCode.OK);
		var result = await _service.GetAddGroupsAsync(storeId);
		await response.WriteAsJsonAsync(result);
		return response;
	}

	//取得商品與口味設定關聯
	[Function("GetCategoryAddonMappings")]
	public async Task<HttpResponseData> GetCategoryAddonMappings(
	[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "store/{storeId}/category-addon-mappings")] HttpRequestData req,
	int storeId,
	FunctionContext context)
	{
		var logger = context.GetLogger("GetCategoryAddonMappings");

		try
		{
			var mappings = await _service.GetCategoryAddonMappingsAsync(storeId);

			var response = req.CreateResponse(HttpStatusCode.OK);
			await response.WriteAsJsonAsync(mappings);
			return response;
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "取得商品類別與加料群組關聯失敗");
			var response = req.CreateResponse(HttpStatusCode.InternalServerError);
			await response.WriteStringAsync("伺服器錯誤");
			return response;
		}
	}




	//新增單一口味群組
	[Function("CreateAddGroup")]
	public async Task<HttpResponseData> CreateAddGroup(
	[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "add-groups")] HttpRequestData req)
	{
		try
		{
			var dto = await req.ReadFromJsonAsync<AddGroupDto>(); // 讀取前端傳來的資料
			if (dto == null || dto.StoreId <= 0 || string.IsNullOrWhiteSpace(dto.GroupName))
			{
				var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
				await badRequest.WriteStringAsync("缺少必要欄位");
				return badRequest;
			}

			int newId = await _service.CreateAddGroupAsync(dto); // 呼叫 service 建立資料

			var response = req.CreateResponse(HttpStatusCode.OK);
			await response.WriteStringAsync("新增成功"); //這裡改為固定訊息
			return response;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "CreateAddGroup 發生錯誤");
			var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
			await errorResponse.WriteStringAsync("伺服器錯誤");
			return errorResponse;
		}
	}




	//新增整單口味
	[Function("CreateOrderFlavor")]
	public async Task<HttpResponseData> CreateOrderFlavor(
	[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "order-flavors")] HttpRequestData req)
	{
		try
		{
			var body = await req.ReadFromJsonAsync<OrderFlavorDto>();
			if (body == null || string.IsNullOrWhiteSpace(body.OptionName) || body.StoreId <= 0)
			{
				var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
				await badRequest.WriteStringAsync("缺少必要欄位");
				return badRequest;
			}

			int newId = await _service.CreateOrderFlavorAsync(body);
			var response = req.CreateResponse(HttpStatusCode.OK);
			await response.WriteStringAsync("新增成功"); //這裡改為固定訊息
			return response;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "CreateOrderFlavor 發生錯誤");
			var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
			await errorResponse.WriteStringAsync("內部錯誤");
			return errorResponse;
		}
	}


	//新增口味
	[Function("CreateAddonItem")]
	public async Task<HttpResponseData> CreateAddonItem(
	[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "add-items")] HttpRequestData req)
	{
		try
		{
			var body = await req.ReadFromJsonAsync<AddItemCreateDto>();

			if (body == null || body.GroupId <= 0 || string.IsNullOrWhiteSpace(body.ItemName))
			{
				var badReq = req.CreateResponse(HttpStatusCode.BadRequest);
				await badReq.WriteStringAsync("缺少必要欄位");
				return badReq;
			}

			int newId = await _service.CreateAddonItemAsync(body);
			var res = req.CreateResponse(HttpStatusCode.OK);
			await res.WriteAsJsonAsync(new { id = newId });
			return res;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "CreateAddonItem 發生錯誤");
			var errorRes = req.CreateResponse(HttpStatusCode.InternalServerError);
			await errorRes.WriteStringAsync("新增失敗");
			return errorRes;
		}
	}



	//取得整張訂單共用口味
	[Function("GetOrderFlavors")]
	public async Task<HttpResponseData> GetOrderFlavors(
		[HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "order-flavors/{storeId:int}")] HttpRequestData req,
		int storeId)
	{
		try
		{
			var result = await _service.GetOrderFlavorsAsync(storeId);
			var response = req.CreateResponse(HttpStatusCode.OK);
			await response.WriteAsJsonAsync(result);
			return response;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "GetOrderFlavors 發生錯誤");
			var error = req.CreateResponse(HttpStatusCode.InternalServerError);
			await error.WriteStringAsync("載入失敗");
			return error;
		}
	}

	//設定口味與商品類別關聯
	[Function("UpdateCategoryAddonMapping")]
	public async Task<HttpResponseData> UpdateCategoryAddonMapping(
		   [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "category-addon-mapping")] HttpRequestData req)
	{
		try
		{
			var dto = await req.ReadFromJsonAsync<CategoryAddGroupMappingDto>();
			if (dto == null || dto.StoreId <= 0 || dto.CategoryId <= 0)
			{
				var badReq = req.CreateResponse(HttpStatusCode.BadRequest);
				await badReq.WriteStringAsync("缺少必要欄位");
				return badReq;
			}

			await _service.UpdateCategoryAddonMappingAsync(dto);

			var response = req.CreateResponse(HttpStatusCode.OK);
			await response.WriteStringAsync("設定成功");
			return response;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "更新商品類別與加料群組關聯失敗");
			var error = req.CreateResponse(HttpStatusCode.InternalServerError);
			await error.WriteStringAsync("內部錯誤");
			return error;
		}
	}




	// 更新整張訂單共用口味狀態
	[Function("UpdateOrderFlavorStatus")]
	public async Task<HttpResponseData> UpdateOrderFlavorStatus(
		[HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "order-flavors/{id}/enabled")] HttpRequestData req,
		int id)
	{
		try
		{
			var body = await req.ReadFromJsonAsync<UpdateFlavorStatusDto>();
			if (body == null)
			{
				var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
				await badRequest.WriteStringAsync("缺少資料");
				return badRequest;
			}

			await _service.UpdateOrderFlavorStatusAsync(id, body.IsEnabled);

			var response = req.CreateResponse(HttpStatusCode.OK);
			await response.WriteStringAsync("狀態已更新");
			return response;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "UpdateOrderFlavorStatus 發生錯誤");
			var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
			await errorResponse.WriteStringAsync("內部錯誤");
			return errorResponse;
		}
	}

	//更新口味類別名稱
	[Function("UpdateAddonGroup")]
	public async Task<HttpResponseData> UpdateAddonGroup(
	[HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "add-groups/{id}")] HttpRequestData req,
	int id)
	{
		try
		{
			var body = await req.ReadFromJsonAsync<AddGroupUpdateDto>();

			if (body == null || string.IsNullOrWhiteSpace(body.GroupName))
			{
				var badReq = req.CreateResponse(HttpStatusCode.BadRequest);
				await badReq.WriteStringAsync("缺少必要欄位");
				return badReq;
			}

			await _service.UpdateAddonGroupAsync(id, body.GroupName);

			var res = req.CreateResponse(HttpStatusCode.OK);
			await res.WriteStringAsync("更新成功");
			return res;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "UpdateAddonGroup 發生錯誤");
			var errorRes = req.CreateResponse(HttpStatusCode.InternalServerError);
			await errorRes.WriteStringAsync("更新失敗");
			return errorRes;
		}
	}




	// 刪除整張訂單共用口味
	[Function("DeleteOrderFlavor")]
	public async Task<HttpResponseData> DeleteOrderFlavor(
		[HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "order-flavors/{flavorId:int}")] HttpRequestData req,
		int flavorId)
	{
		try
		{
			var success = await _service.DeleteOrderFlavorAsync(flavorId);
			if (success)
			{
				var response = req.CreateResponse(HttpStatusCode.OK);
				await response.WriteStringAsync("刪除成功");
				return response;
			}
			else
			{
				var notFound = req.CreateResponse(HttpStatusCode.NotFound);
				await notFound.WriteStringAsync("找不到資料");
				return notFound;
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "DeleteOrderFlavor 發生錯誤");
			var error = req.CreateResponse(HttpStatusCode.InternalServerError);
			await error.WriteStringAsync("刪除失敗");
			return error;
		}
	}


	//刪除口味及其群組
	[Function("DeleteAddGroup")]
	public async Task<HttpResponseData> DeleteAddGroup(
	[HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "add-groups/{groupId:int}")] HttpRequestData req,
	int groupId)
	{
		try
		{
			await _service.DeleteAddGroupWithItemsAsync(groupId); // 呼叫服務層邏輯

			var response = req.CreateResponse(HttpStatusCode.OK);
			await response.WriteStringAsync("刪除成功");
			return response;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "刪除加料群組失敗");
			var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
			await errorResponse.WriteStringAsync("刪除失敗");
			return errorResponse;
		}
	}


	//刪除單一口味
	[Function("DeleteAddonItem")]
	public async Task<HttpResponseData> DeleteAddonItem(
		[HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "add-items/{itemId:int}")] HttpRequestData req,
		int itemId)
	{
		try
		{
			bool success = await _service.DeleteAddonItemAsync(itemId);
			var response = req.CreateResponse(success ? HttpStatusCode.OK : HttpStatusCode.NotFound);
			await response.WriteStringAsync(success ? "刪除成功" : "找不到此加料項目");
			return response;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "刪除加料項目失敗");
			var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
			await errorResponse.WriteStringAsync("內部錯誤");
			return errorResponse;
		}
	}

}