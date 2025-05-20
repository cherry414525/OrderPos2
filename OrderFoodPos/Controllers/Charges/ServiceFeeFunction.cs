using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using OrderFoodPos.Services.Charges;
using OrderFoodPos.Models.Charges;
using System.Net;
using System.Text.Json;

namespace OrderFoodPos.Controllers.Charges
{
    public class ServiceFeeFunction
    {
        private readonly ILogger<ServiceFeeFunction> _logger;
        private readonly ServiceFeeService _service;

        public ServiceFeeFunction(ILogger<ServiceFeeFunction> logger, ServiceFeeService service)
        {
            _logger = logger;
            _service = service;
        }

        /// <summary>
        /// ���o���w�ө����A�ȶO�]�w
        /// </summary>
        [Function("GetServiceFeeSettings")]
        public async Task<HttpResponseData> GetServiceFeeSettings(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "charges/servicefee/get")] HttpRequestData req)
        {
            var response = req.CreateResponse();

            try
            {
                var body = await req.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<Dictionary<string, int>>(body);

                if (!data.TryGetValue("StoreId", out int storeId) || storeId <= 0)
                {
                    response.StatusCode = HttpStatusCode.BadRequest;
                    await response.WriteStringAsync("�L�Ī� StoreId");
                    return response;
                }

                var result = await _service.GetServiceFeeByStoreId(storeId);

                if (result == null)
                {
                    response.StatusCode = HttpStatusCode.NotFound;
                    await response.WriteStringAsync("�d�L�A�ȶO�]�w");
                    return response;
                }

                response.StatusCode = HttpStatusCode.OK;
                await response.WriteAsJsonAsync(result);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "���o�A�ȶO�]�w�ɵo�Ϳ��~");

                response.StatusCode = HttpStatusCode.InternalServerError;
                await response.WriteStringAsync($"���A�����~�G{ex.Message}");
                return response;
            }
        }


        /// <summary>
        /// �]�w�ө����A�ȶO
        /// </summary>
        [Function("UpdateServiceFeeSettings")]
        public async Task<HttpResponseData> UpsertServiceFeeSettings(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "charges/servicefee/update")] HttpRequestData req)
        {
            var response = req.CreateResponse();

            try
            {
                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var model = await JsonSerializer.DeserializeAsync<ServiceFeeModel>(req.Body, jsonOptions);

                if (model == null || model.StoreId <= 0)
                {
                    response.StatusCode = HttpStatusCode.BadRequest;
                    await response.WriteStringAsync("�L�Ī��A�ȶO�]�w���");
                    return response;
                }

                await _service.UpsertServiceFeeSettingAsync(model);

                response.StatusCode = HttpStatusCode.OK;
                await response.WriteStringAsync("�A�ȶO�]�w�w�x�s");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "��s�A�ȶO�]�w����");

                response.StatusCode = HttpStatusCode.InternalServerError;
                await response.WriteStringAsync("�x�s���ѡG" + ex.Message);
                return response;
            }
        }
    }
}
