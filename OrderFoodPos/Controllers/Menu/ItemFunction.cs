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
		var result = await _service.GetByIdAsync(id); // �d�߸��
		if (result == null)
			return new NotFoundObjectResult($"�䤣���\�I id: {id}"); // �Y�d�L��Ʀ^�� 404

		return new OkObjectResult(result); // ���\�^�Ǹ��
	}


	[Function("GetMenuItemsByStore")]
	public async Task<IActionResult> GetMenuItemsByStore(
	[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "menuitems/get")] HttpRequestData req)
	{
		var body = await req.ReadAsStringAsync(); // Ū�� JSON
		var data = JsonSerializer.Deserialize<Dictionary<string, int>>(body);

		if (!data.TryGetValue("StoreId", out int StoreId) || StoreId <= 0)
			return new BadRequestObjectResult("storeId �L��");

		var result = await _service.GetAllByStoreIdAsync(StoreId); // �I�s Service �h���o���
		return new OkObjectResult(result); // �^���\�I�C��
	}




	/// <summary>
	/// �s�W�\�I API�]�ϥΤw�W�Ǫ��Ϥ� URL�^
	/// </summary>
	[Function("CreateMenuItem")]
	public async Task<HttpResponseData> CreateMenuItem(
		[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "Menu/items")] HttpRequestData req,
		FunctionContext executionContext)
	{
		var logger = executionContext.GetLogger("CreateMenuItem");
		logger.LogInformation("����s�W�\�I�ШD");

		try
		{
			// �]�w JSON �ﶵ�H�����j�p�g
			var jsonOptions = new JsonSerializerOptions
			{
				PropertyNameCaseInsensitive = true,
				DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
			};

			// �q�ШD���ϧǦC�� MenuItem ����
			var model = await JsonSerializer.DeserializeAsync<MenuItem>(req.Body, jsonOptions);

			// ���Ұ򥻥������
			if (model == null || string.IsNullOrWhiteSpace(model.Name))
			{
				var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
				await badResponse.WriteStringAsync("�\�I�W�٬��������");
				return badResponse;
			}

			// �p�G�S�����w Sequence�A�]�m�@�ӹw�]��
			if (model.Sequence <= 0)
			{
				model.Sequence = 999; // �w�]��b�̫�
			}

			// �I�s Service �h�s�W�\�I
			var newId = await _service.AddAsync(model);

			// �إߦ��\�^��
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
			logger.LogError(ex, "�s�W�\�I�ɵo�Ϳ��~");

			// �إ߿��~�^��
			var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
			await errorResponse.WriteStringAsync($"�B�z�ШD�ɵo�Ϳ��~: {ex.Message}");
			return errorResponse;
		}
	}


	/// <summary>
	/// ����W�ǹϤ��Ϊ� SAS �O�P
	/// </summary>
	[Function("GetUploadSasToken")]
	public async Task<HttpResponseData> GetUploadSasToken(
		[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "Menu/getUploadSasToken")] HttpRequestData req,
		FunctionContext executionContext)
	{
		var logger = executionContext.GetLogger("GetUploadSasToken");
		logger.LogInformation("������� SAS �O�P�ШD");

		try
		{
			// �q�ШD������ө� ID
			var requestBody = await req.ReadAsStringAsync();
			var data = JsonSerializer.Deserialize<Dictionary<string, int>>(requestBody);

			if (!data.TryGetValue("StoreId", out int storeId) || storeId <= 0)
			{
				var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
				await badResponse.WriteStringAsync("�L�Ī��ө� ID");
				return badResponse;
			}

			// �ͦ� SAS �O�P
			var sasInfo = await _service.GenerateUploadSasToken(storeId);

			// ��^ SAS �H��
			var response = req.CreateResponse(HttpStatusCode.OK);
			await response.WriteAsJsonAsync(sasInfo);
			return response;
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "�ͦ� SAS �O�P�ɵo�Ϳ��~");
			var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
			await errorResponse.WriteStringAsync($"�B�z�ШD�ɵo�Ϳ��~: {ex.Message}");
			return errorResponse;
		}
	}


	[Function("ImportMenuData")]
	public async Task<HttpResponseData> ImportMenuData(
	[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "menu/import")] HttpRequestData req)
	{
		_logger.LogInformation("������פJ�ШD");

		var body = await req.ReadAsStringAsync();
		var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
		var request = JsonSerializer.Deserialize<ImportMenuRequest>(body, options);

		if (request == null || request.StoreId <= 0)
		{
			var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
			await badResponse.WriteStringAsync("�ʤ֩εL�Ī� StoreId");
			return badResponse;
		}

		try
		{
			//�p�G�O�л\�Ҧ��A���M�Ÿ��
			if (request.Overwrite)
			{
				await _service.DeleteAllCategoriesAndItemsByStoreAsync(request.StoreId);
			}

			//�إߤ����A�ðO��s�W�� Id ���������W��
			var createdCategories = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase); // ���Ϥ��j�p�g

			foreach (var category in request.Categories)
			{
				if (string.IsNullOrWhiteSpace(category.Name))
				{
					_logger.LogWarning("�פJ�ɵo�{�ť����O�W�١A�w���L");
					continue;
				}

				category.StoreId = request.StoreId;
				var id = await _service.CreateAsync(category);
				createdCategories[category.Name.Trim()] = id;
			}

			//�إ��\�I���
			foreach (var itemDto in request.MenuItems)
			{
				if (string.IsNullOrWhiteSpace(itemDto.CategoryName))
				{
					_logger.LogWarning($"�\�I {itemDto.Name} �S������ CategoryName�A�w���L");
					continue;
				}

				if (!createdCategories.TryGetValue(itemDto.CategoryName.Trim(), out int categoryId))
				{
					_logger.LogWarning($"�䤣��������O: {itemDto.CategoryName}");
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
			await response.WriteStringAsync("�פJ���\");
			return response;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "���פJ�ɵo�Ϳ��~");
			var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
			await errorResponse.WriteStringAsync("�B�z���פJ���ѡG" + ex.Message);
			return errorResponse;
		}
	}


	[Function("DeleteMenuItem")]
	public async Task<HttpResponseData> DeleteMenuItem(
	[HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "menuitems/{id:int}")] HttpRequestData req,
	int id)
	{
		_logger.LogInformation($"����R���\�I���ШD�AID: {id}");

		try
		{
			await _service.DeleteMenuItemAsync(id); // �I�s Service �R����k

			var response = req.CreateResponse(HttpStatusCode.OK);
			await response.WriteStringAsync("�R�����\");
			return response;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, $"�R���\�I���ѡAID: {id}");
			var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
			await errorResponse.WriteStringAsync($"�R������: {ex.Message}");
			return errorResponse;
		}
	}

}