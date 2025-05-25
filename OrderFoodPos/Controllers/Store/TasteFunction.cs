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

	//��o���a�f���]�w
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

	//���o�ӫ~�P�f���]�w���p
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
			logger.LogError(ex, "���o�ӫ~���O�P�[�Ƹs�����p����");
			var response = req.CreateResponse(HttpStatusCode.InternalServerError);
			await response.WriteStringAsync("���A�����~");
			return response;
		}
	}




	//�s�W��@�f���s��
	[Function("CreateAddGroup")]
	public async Task<HttpResponseData> CreateAddGroup(
	[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "add-groups")] HttpRequestData req)
	{
		try
		{
			var dto = await req.ReadFromJsonAsync<AddGroupDto>(); // Ū���e�ݶǨӪ����
			if (dto == null || dto.StoreId <= 0 || string.IsNullOrWhiteSpace(dto.GroupName))
			{
				var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
				await badRequest.WriteStringAsync("�ʤ֥��n���");
				return badRequest;
			}

			int newId = await _service.CreateAddGroupAsync(dto); // �I�s service �إ߸��

			var response = req.CreateResponse(HttpStatusCode.OK);
			await response.WriteStringAsync("�s�W���\"); //�o�̧אּ�T�w�T��
			return response;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "CreateAddGroup �o�Ϳ��~");
			var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
			await errorResponse.WriteStringAsync("���A�����~");
			return errorResponse;
		}
	}




	//�s�W���f��
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
				await badRequest.WriteStringAsync("�ʤ֥��n���");
				return badRequest;
			}

			int newId = await _service.CreateOrderFlavorAsync(body);
			var response = req.CreateResponse(HttpStatusCode.OK);
			await response.WriteStringAsync("�s�W���\"); //�o�̧אּ�T�w�T��
			return response;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "CreateOrderFlavor �o�Ϳ��~");
			var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
			await errorResponse.WriteStringAsync("�������~");
			return errorResponse;
		}
	}


	//�s�W�f��
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
				await badReq.WriteStringAsync("�ʤ֥��n���");
				return badReq;
			}

			int newId = await _service.CreateAddonItemAsync(body);
			var res = req.CreateResponse(HttpStatusCode.OK);
			await res.WriteAsJsonAsync(new { id = newId });
			return res;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "CreateAddonItem �o�Ϳ��~");
			var errorRes = req.CreateResponse(HttpStatusCode.InternalServerError);
			await errorRes.WriteStringAsync("�s�W����");
			return errorRes;
		}
	}



	//���o��i�q��@�Τf��
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
			_logger.LogError(ex, "GetOrderFlavors �o�Ϳ��~");
			var error = req.CreateResponse(HttpStatusCode.InternalServerError);
			await error.WriteStringAsync("���J����");
			return error;
		}
	}

	//�]�w�f���P�ӫ~���O���p
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
				await badReq.WriteStringAsync("�ʤ֥��n���");
				return badReq;
			}

			await _service.UpdateCategoryAddonMappingAsync(dto);

			var response = req.CreateResponse(HttpStatusCode.OK);
			await response.WriteStringAsync("�]�w���\");
			return response;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "��s�ӫ~���O�P�[�Ƹs�����p����");
			var error = req.CreateResponse(HttpStatusCode.InternalServerError);
			await error.WriteStringAsync("�������~");
			return error;
		}
	}




	// ��s��i�q��@�Τf�����A
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
				await badRequest.WriteStringAsync("�ʤָ��");
				return badRequest;
			}

			await _service.UpdateOrderFlavorStatusAsync(id, body.IsEnabled);

			var response = req.CreateResponse(HttpStatusCode.OK);
			await response.WriteStringAsync("���A�w��s");
			return response;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "UpdateOrderFlavorStatus �o�Ϳ��~");
			var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
			await errorResponse.WriteStringAsync("�������~");
			return errorResponse;
		}
	}

	//��s�f�����O�W��
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
				await badReq.WriteStringAsync("�ʤ֥��n���");
				return badReq;
			}

			await _service.UpdateAddonGroupAsync(id, body.GroupName);

			var res = req.CreateResponse(HttpStatusCode.OK);
			await res.WriteStringAsync("��s���\");
			return res;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "UpdateAddonGroup �o�Ϳ��~");
			var errorRes = req.CreateResponse(HttpStatusCode.InternalServerError);
			await errorRes.WriteStringAsync("��s����");
			return errorRes;
		}
	}




	// �R����i�q��@�Τf��
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
				await response.WriteStringAsync("�R�����\");
				return response;
			}
			else
			{
				var notFound = req.CreateResponse(HttpStatusCode.NotFound);
				await notFound.WriteStringAsync("�䤣����");
				return notFound;
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "DeleteOrderFlavor �o�Ϳ��~");
			var error = req.CreateResponse(HttpStatusCode.InternalServerError);
			await error.WriteStringAsync("�R������");
			return error;
		}
	}


	//�R���f���Ψ�s��
	[Function("DeleteAddGroup")]
	public async Task<HttpResponseData> DeleteAddGroup(
	[HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "add-groups/{groupId:int}")] HttpRequestData req,
	int groupId)
	{
		try
		{
			await _service.DeleteAddGroupWithItemsAsync(groupId); // �I�s�A�ȼh�޿�

			var response = req.CreateResponse(HttpStatusCode.OK);
			await response.WriteStringAsync("�R�����\");
			return response;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "�R���[�Ƹs�ե���");
			var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
			await errorResponse.WriteStringAsync("�R������");
			return errorResponse;
		}
	}


	//�R����@�f��
	[Function("DeleteAddonItem")]
	public async Task<HttpResponseData> DeleteAddonItem(
		[HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "add-items/{itemId:int}")] HttpRequestData req,
		int itemId)
	{
		try
		{
			bool success = await _service.DeleteAddonItemAsync(itemId);
			var response = req.CreateResponse(success ? HttpStatusCode.OK : HttpStatusCode.NotFound);
			await response.WriteStringAsync(success ? "�R�����\" : "�䤣�즹�[�ƶ���");
			return response;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "�R���[�ƶ��إ���");
			var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
			await errorResponse.WriteStringAsync("�������~");
			return errorResponse;
		}
	}

}