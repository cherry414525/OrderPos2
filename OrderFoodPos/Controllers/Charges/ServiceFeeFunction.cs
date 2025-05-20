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
        /// 取得指定商店的服務費設定
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
                    await response.WriteStringAsync("無效的 StoreId");
                    return response;
                }

                var result = await _service.GetServiceFeeByStoreId(storeId);

                if (result == null)
                {
                    response.StatusCode = HttpStatusCode.NotFound;
                    await response.WriteStringAsync("查無服務費設定");
                    return response;
                }

                response.StatusCode = HttpStatusCode.OK;
                await response.WriteAsJsonAsync(result);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得服務費設定時發生錯誤");

                response.StatusCode = HttpStatusCode.InternalServerError;
                await response.WriteStringAsync($"伺服器錯誤：{ex.Message}");
                return response;
            }
        }


        /// <summary>
        /// 設定商店的服務費
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
                    await response.WriteStringAsync("無效的服務費設定資料");
                    return response;
                }

                await _service.UpsertServiceFeeSettingAsync(model);

                response.StatusCode = HttpStatusCode.OK;
                await response.WriteStringAsync("服務費設定已儲存");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新服務費設定失敗");

                response.StatusCode = HttpStatusCode.InternalServerError;
                await response.WriteStringAsync("儲存失敗：" + ex.Message);
                return response;
            }
        }
    }
}
