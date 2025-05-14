using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using OrderFoodPos.Models.Store;
using OrderFoodPos.Repositories.Store;
using System.Net;
using System.Text.Json;


namespace OrderFoodPos.Controllers.Store
{
    public class StoreFunction
    {
        private readonly ILogger<StoreFunction> _logger;
		private readonly StoresRepository _storeRepo;

		public StoreFunction(ILogger<StoreFunction> logger,StoresRepository storeRepo)
        {
            _logger = logger;
			_storeRepo = storeRepo;
		}

		//����Ӯa���
		[Function("GetStoreById")]
		public async Task<HttpResponseData> GetStoreById(
	[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "store/get")] HttpRequestData req)
		{
			var response = req.CreateResponse();

			// Ū�� body
			string body = await new StreamReader(req.Body).ReadToEndAsync();
			var data = JsonSerializer.Deserialize<GetStoreRequest>(body);

			if (data == null || data.StoreId <= 0)
			{
				response.StatusCode = HttpStatusCode.BadRequest;
				await response.WriteAsJsonAsync(new { message = "�д��Ѧ��Ī� StoreId" });
				return response;
			}

			var store = await _storeRepo.GetStoreByIdAsync(data.StoreId);
			if (store == null)
			{
				response.StatusCode = HttpStatusCode.NotFound;
				await response.WriteAsJsonAsync(new { message = "�䤣��Ӯa���" });
				return response;
			}

			response.StatusCode = HttpStatusCode.OK;
			await response.WriteAsJsonAsync(store);
			return response;
		}




		//��s�Ӯa���
		[Function("UpdateStoreInfo")]
		public async Task<HttpResponseData> UpdateStoreInfoAsync(
	[HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "store/update")] HttpRequestData req)
		{
			var response = req.CreateResponse();

			string body = await new StreamReader(req.Body).ReadToEndAsync();
			var data = JsonSerializer.Deserialize<StoreUpdateRequest>(body);

			if (data == null || data.Id <= 0)
			{
				response.StatusCode = HttpStatusCode.BadRequest;
				await response.WriteAsJsonAsync(new { message = "�д��ѥ��T����ƻP�ө� ID" });
				return response;
			}

			bool success = await _storeRepo.UpdateStoreAsync(data);
			if (!success)
			{
				response.StatusCode = HttpStatusCode.InternalServerError;
				await response.WriteAsJsonAsync(new { message = "��s���ѡA�нT�{�ө� ID �O�_���T" });
				return response;
			}

			response.StatusCode = HttpStatusCode.OK;
			await response.WriteAsJsonAsync(new { message = "��s���\" });
			return response;
		}

	}
}
