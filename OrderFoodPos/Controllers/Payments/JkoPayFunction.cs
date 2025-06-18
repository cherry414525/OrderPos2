using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using OrderFoodPos.Models.Payments;
using OrderFoodPos.Services;

namespace OrderFoodPos.Functions
{
    public class JkoPayController
    {
        private readonly ILogger<JkoPayController> _logger;
        private readonly JkoPayService _jkoPayService;

        public JkoPayController(ILogger<JkoPayController> logger, JkoPayService jkoPayService)
        {
            _logger = logger;
            _jkoPayService = jkoPayService;
        }

        [Function("RequestJkoPayPayment")]
        public async Task<HttpResponseData> RequestPaymentAsync(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "jkopay/payment")] HttpRequestData req)
        {
            var response = req.CreateResponse();

            try
            {
                string body = await req.ReadAsStringAsync();
                _logger.LogInformation("接收到街口付款請求：{Body}", body);

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var request = JsonSerializer.Deserialize<JkoPayRequest>(body, options);

                if (request == null)
                {
                    response.StatusCode = HttpStatusCode.BadRequest;
                    await response.WriteStringAsync("無效的請求資料");
                    return response;
                }

                // 檢查必填欄位（文件中所有必要欄位）
                if (string.IsNullOrWhiteSpace(request.CardToken) ||
                    string.IsNullOrWhiteSpace(request.MerchantTradeNo) ||
                    string.IsNullOrWhiteSpace(request.PosID) ||
                    string.IsNullOrWhiteSpace(request.StoreID) ||
                    string.IsNullOrWhiteSpace(request.StoreName) ||
                    request.TradeAmount <= 0)
                {
                    response.StatusCode = HttpStatusCode.BadRequest;
                    await response.WriteStringAsync("缺少必要欄位");
                    return response;
                }

                var resultJson = await _jkoPayService.RequestPaymentAsync(request);

                response.StatusCode = HttpStatusCode.OK;
                response.Headers.Add("Content-Type", "application/json; charset=utf-8");
                await response.WriteStringAsync(resultJson);

                return response;
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "街口付款發生例外錯誤");
                response.StatusCode = HttpStatusCode.InternalServerError;
                await response.WriteStringAsync("伺服器錯誤");
                return response;
            }
        }


        [Function("CancelJkoPayPayment")]
        public async Task<HttpResponseData> CancelPaymentAsync(
    [HttpTrigger(AuthorizationLevel.Function, "post", Route = "jkopay/cancel")] HttpRequestData req)
        {
            var response = req.CreateResponse();

            try
            {
                string body = await req.ReadAsStringAsync();
                _logger.LogInformation("接收到街口取消付款請求：{Body}", body);

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var request = JsonSerializer.Deserialize<JkoPayCancelRequest>(body, options);

                if (request == null)
                {
                    response.StatusCode = HttpStatusCode.BadRequest;
                    await response.WriteStringAsync("無效的請求資料");
                    return response;
                }

                // 檢查必填欄位
                if (string.IsNullOrWhiteSpace(request.CardToken) ||
                    string.IsNullOrWhiteSpace(request.MerchantTradeNo) ||
                    string.IsNullOrWhiteSpace(request.PosID) ||
                    string.IsNullOrWhiteSpace(request.StoreID) ||
                    string.IsNullOrWhiteSpace(request.StoreName) ||
                    request.TradeAmount < 0)
                {
                    response.StatusCode = HttpStatusCode.BadRequest;
                    await response.WriteStringAsync("缺少必要欄位");
                    return response;
                }

                var resultJson = await _jkoPayService.CancelPaymentAsync(request);

                response.StatusCode = HttpStatusCode.OK;
                response.Headers.Add("Content-Type", "application/json; charset=utf-8");
                await response.WriteStringAsync(resultJson);

                return response;
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "街口取消付款發生例外錯誤");
                response.StatusCode = HttpStatusCode.InternalServerError;
                await response.WriteStringAsync("伺服器錯誤");
                return response;
            }
        }


    }
}
