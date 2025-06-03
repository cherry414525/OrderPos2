using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using OrderFoodPos.Models;
using OrderFoodPos.Models.Orders;
using OrderFoodPos.Repositories.Orders;
using OrderFoodPos.Services;
using OrderFoodPos.Services.Orders;

namespace OrderFoodPos.Functions
{
    public class LinePayFunction
    {
        private readonly ILogger<LinePayFunction> _logger;
        private readonly LinePayService _linePayService;
        private readonly OrderService _orderService;

        // 建構子，注入 Logger 與 LinePayService
        public LinePayFunction(ILogger<LinePayFunction> logger, LinePayService linePayService, OrderService orderService)
        {
            _logger = logger;
            _linePayService = linePayService;
            _orderService = orderService;
        }

        // 處理 LinePay 的付款請求
        [Function("RequestLinePayPayment")]
        public async Task<HttpResponseData> RequestPaymentAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "linepay/request")] HttpRequestData req)
        {
            var response = req.CreateResponse();

            try
            {
                // 讀取請求的 Body
                var body = await req.ReadAsStringAsync();
                _logger.LogInformation("收到 LinePay 請求：{Body}", body);

                // 將 JSON 字串反序列化為物件
                var linePayRequest = JsonSerializer.Deserialize<LinePayRequest>(body, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                // 無論有沒有傳入，都產生新的 orderId（覆蓋）
                linePayRequest.orderId = "order" + DateTime.Now.ToString("yyyyMMdd") + Guid.NewGuid().ToString("N").Substring(0, 8);

                // 檢查請求是否為空
                if (linePayRequest == null)
                {
                    response.StatusCode = HttpStatusCode.BadRequest;
                    await response.WriteStringAsync("請求內容無效");
                    return response;
                }

                // ✅ 檢查必填欄位是否存在
                if (string.IsNullOrWhiteSpace(linePayRequest.packageName) ||
                    linePayRequest.redirectUrls == null ||
                    string.IsNullOrWhiteSpace(linePayRequest.redirectUrls.confirmUrl) ||
                    string.IsNullOrWhiteSpace(linePayRequest.redirectUrls.cancelUrl))
                {
                    response.StatusCode = HttpStatusCode.BadRequest;
                    await response.WriteStringAsync("缺少必要欄位：packageName 或 redirectUrls");
                    return response;
                }

                // 呼叫服務進行付款請求
                var resultJson = await _linePayService.RequestPaymentAsync(linePayRequest);

                // 這裡可以先檢查 LinePay API 回傳是否成功 (視你的 _linePayService 實作)
                await _orderService.CreateOrderAsync(linePayRequest);

                response.StatusCode = HttpStatusCode.OK;
                response.Headers.Add("Content-Type", "application/json");
                await response.WriteStringAsync(resultJson);

                return response;
            }
            catch (Exception ex)
            {
                // 發生例外時記錄錯誤
                _logger.LogError(ex, "呼叫 LinePay API 發生錯誤");
                response.StatusCode = HttpStatusCode.InternalServerError;
                await response.WriteStringAsync("伺服器錯誤，請稍後再試");
                return response;
            }
        }

        // 確認付款（LINE Pay 在付款完成後會重導向到這裡）
        [Function("ConfirmLinePayPayment")]
        public async Task<HttpResponseData> ConfirmPaymentAsync(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "linepay/confirm")] HttpRequestData req)
        {
            var response = req.CreateResponse();

            try
            {
                var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
                var transactionId = query["transactionId"];
                var orderId = query["orderId"];

                if (string.IsNullOrEmpty(transactionId) || string.IsNullOrEmpty(orderId))
                {
                    response.StatusCode = HttpStatusCode.BadRequest;
                    await response.WriteStringAsync("缺少 transactionId 或 orderId");
                    return response;
                }

                
                // 根據 orderId 查詢訂單資訊(查金額)
                var order = await _orderService.GetOrderAsync(orderId);
                if (order == null)
                {
                    response.StatusCode = HttpStatusCode.NotFound;
                    await response.WriteStringAsync("找不到訂單");
                    return response;
                }

                int amount = order.TotalAmount;


                // 呼叫 LinePay API 確認付款
                var result = await _linePayService.ConfirmPaymentAsync(transactionId, 200);

                // 更新訂單狀態
                await _orderService.UpdateOrderStatusAsync(orderId, "Paid");
                // 寫入付款紀錄
                await _orderService.AddPaymentAsync(orderId, transactionId, order.TotalAmount);


                response.StatusCode = HttpStatusCode.OK;
                await response.WriteStringAsync(result);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "確認付款發生錯誤");
                response.StatusCode = HttpStatusCode.InternalServerError;
                await response.WriteStringAsync("伺服器錯誤");
                return response;
            }
        }


        // 查詢付款交易紀錄
        [Function("QueryLinePayPayment")]
        public async Task<HttpResponseData> QueryPaymentAsync(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "linepay/query")] HttpRequestData req)
        {
            var response = req.CreateResponse();

            try
            {
                var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
                var transactionId = query["transactionId"];
                var orderId = query["orderId"];

                var transactionIds = string.IsNullOrEmpty(transactionId) ? null : new[] { transactionId };
                var orderIds = string.IsNullOrEmpty(orderId) ? null : new[] { orderId };

                var result = await _linePayService.GetPaymentDetailsAsync(transactionIds, orderIds);

                response.StatusCode = HttpStatusCode.OK;
                response.Headers.Add("Content-Type", "application/json");
                await response.WriteStringAsync(result);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查詢交易發生錯誤");
                response.StatusCode = HttpStatusCode.InternalServerError;
                await response.WriteStringAsync("伺服器錯誤");
                return response;
            }
        }


        // 處理退款請求
        [Function("RefundLinePayPayment")]
        public async Task<HttpResponseData> RefundPaymentAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "linepay/refund")] HttpRequestData req)
        {
            var response = req.CreateResponse();

            try
            {
                // 讀取退款請求的內容
                var body = await req.ReadAsStringAsync();
                var refundRequest = JsonSerializer.Deserialize<LinePayRefundRequest>(body, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                // 檢查必要欄位
                if (refundRequest == null || string.IsNullOrEmpty(refundRequest.transactionId))
                {
                    response.StatusCode = HttpStatusCode.BadRequest;
                    await response.WriteStringAsync("缺少 transactionId");
                    return response;
                }

                // 呼叫服務執行退款
                var result = await _linePayService.RefundPaymentAsync(refundRequest.transactionId, refundRequest.refundAmount);

                response.StatusCode = HttpStatusCode.OK;
                await response.WriteStringAsync(result);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "退款時發生錯誤");
                response.StatusCode = HttpStatusCode.InternalServerError;
                await response.WriteStringAsync("伺服器錯誤");
                return response;
            }
        }
    }
}

