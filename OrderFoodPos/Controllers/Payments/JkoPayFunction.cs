using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using OrderFoodPos.Models.Payments;
using OrderFoodPos.Services;
using OrderFoodPos.Services.Orders;
using OrderFoodPos.Services.Payments;

namespace OrderFoodPos.Functions
{
    public class JkoPayFunction
    {
        private readonly ILogger<JkoPayFunction> _logger;
        private readonly JkoPayService _jkoPayService;
        private readonly OrderService _orderService;

        public JkoPayFunction(ILogger<JkoPayFunction> logger, JkoPayService jkoPayService, OrderService orderService)
        {
            _logger = logger;
            _jkoPayService = jkoPayService;
            _orderService = orderService;
        }

        // 處理街口付款請求
        [Function("RequestJkoPayPayment")]
        public async Task<HttpResponseData> RequestPaymentAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "jkopay/request")] HttpRequestData req)
        {
            var response = req.CreateResponse();

            try
            {
                // 讀取請求的 JSON 字串
                var body = await req.ReadAsStringAsync();
                _logger.LogInformation("收到 JKO Pay 請求：{Body}", body);

                var jkoRequest = JsonSerializer.Deserialize<JkoPayRequest>(body, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                // 檢查請求是否為空
                if (jkoRequest == null)
                {
                    response.StatusCode = HttpStatusCode.BadRequest;
                    await response.WriteStringAsync("請求內容無效");
                    return response;
                }

                // 檢查必填欄位
                if (jkoRequest.amount <= 0 ||
                    string.IsNullOrWhiteSpace(jkoRequest.notifyUrl) ||
                    string.IsNullOrWhiteSpace(jkoRequest.returnUrl) ||
                    string.IsNullOrWhiteSpace(jkoRequest.description))
                {
                    response.StatusCode = HttpStatusCode.BadRequest;
                    await response.WriteStringAsync("缺少必要欄位：Amount, NotifyUrl, ReturnUrl 或 Description");
                    return response;
                }

                // 產生訂單編號
                jkoRequest.orderId = "jko" + DateTime.Now.ToString("yyyyMMdd") + Guid.NewGuid().ToString("N").Substring(0, 8);

                // 呼叫服務進行付款請求
                var resultJson = await _jkoPayService.RequestPaymentAsync(jkoRequest);

                // 建立訂單紀錄（視實作而定）
                //await _orderService.CreateOrderAsync(jkoRequest); // 確保你有這個方法重載支援 JkoPayRequest

                response.StatusCode = HttpStatusCode.OK;
                response.Headers.Add("Content-Type", "application/json");
                await response.WriteStringAsync(resultJson);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "呼叫街口 API 發生錯誤");
                response.StatusCode = HttpStatusCode.InternalServerError;
                await response.WriteStringAsync("伺服器錯誤，請稍後再試");
                return response;
            }
        }
    }
}
