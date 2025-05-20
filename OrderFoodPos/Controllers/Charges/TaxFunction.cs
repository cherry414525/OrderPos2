using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using OrderFoodPos.Services.Charges;
using OrderFoodPos.Models.Charges;
using System.Net;
using System.Text.Json;

namespace OrderFoodPos.Controllers.Charges
{
    public class TaxFunction
    {
        private readonly ILogger<TaxFunction> _logger;
        private readonly TaxService _service;

        public TaxFunction(ILogger<TaxFunction> logger, TaxService service)
        {
            _logger = logger;
            _service = service;
        }

        [Function("GetTaxSettings")]
        public async Task<HttpResponseData> GetTaxSettings(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "charges/tax/get")] HttpRequestData req)
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

                var result = await _service.GetTaxByStoreId(storeId);

                if (result == null)
                {
                    response.StatusCode = HttpStatusCode.NotFound;
                    await response.WriteStringAsync("�d�L�|�v�]�w");
                    return response;
                }

                response.StatusCode = HttpStatusCode.OK;
                await response.WriteAsJsonAsync(result);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "���o�|�v�]�w�ɵo�Ϳ��~");

                response.StatusCode = HttpStatusCode.InternalServerError;
                await response.WriteStringAsync($"���A�����~�G{ex.Message}");
                return response;
            }
        }


        //�]�w�Ӯa�|�v
        [Function("UpdateTaxSettings")]
        public async Task<HttpResponseData> UpsertTaxSettings(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "charges/tax/update")] HttpRequestData req)
        {
            var logger = _logger;
            try
            {
                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var model = await JsonSerializer.DeserializeAsync<TaxModel>(req.Body, jsonOptions);

                if (model == null || model.StoreId <= 0)
                {
                    var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badResponse.WriteStringAsync("�L�Ī��|�ȳ]�w���");
                    return badResponse;
                }

                await _service.UpsertTaxSettingAsync(model);

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteStringAsync("�|�ȳ]�w�w�x�s");
                return response;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "��s�|�ȳ]�w����");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync("�x�s���ѡG" + ex.Message);
                return errorResponse;
            }
        }


    }
}
