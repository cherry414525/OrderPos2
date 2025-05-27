using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using OrderFoodPos.Models;
using OrderFoodPos.Services;

namespace OrderFoodPos.Functions
{
    public class LinePayFunction
    {
        private readonly ILogger<LinePayFunction> _logger;
        private readonly LinePayService _linePayService;

        public LinePayFunction(ILogger<LinePayFunction> logger, LinePayService linePayService)
        {
            _logger = logger;
            _linePayService = linePayService;
        }



        [Function("RequestLinePayPayment")]
        public async Task<HttpResponseData> RequestPaymentAsync(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "linepay/request")] HttpRequestData req)
        {
            var response = req.CreateResponse();

            try
            {
                var body = await req.ReadAsStringAsync();
                _logger.LogInformation("收到 LinePay 請求：{Body}", body);
                var linePayRequest = JsonSerializer.Deserialize<LinePayRequest>(body, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (linePayRequest == null)
                {
                    response.StatusCode = HttpStatusCode.BadRequest;
                    await response.WriteStringAsync("請求內容無效");
                    return response;
                }

                // ✅ 檢查必填欄位
                if (string.IsNullOrWhiteSpace(linePayRequest.packageName) ||
                    linePayRequest.redirectUrls == null ||
                    string.IsNullOrWhiteSpace(linePayRequest.redirectUrls.confirmUrl) ||
                    string.IsNullOrWhiteSpace(linePayRequest.redirectUrls.cancelUrl))
                {
                    response.StatusCode = HttpStatusCode.BadRequest;
                    await response.WriteStringAsync("缺少必要欄位：packageName 或 redirectUrls");
                    return response;
                }

                // 直接把 linePayRequest 傳給服務，讓它負責序列化和簽名
                var resultJson = await _linePayService.RequestPaymentAsync(linePayRequest);

                response.StatusCode = HttpStatusCode.OK;
                response.Headers.Add("Content-Type", "application/json");
                await response.WriteStringAsync(resultJson);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "呼叫 LinePay API 發生錯誤");
                response.StatusCode = HttpStatusCode.InternalServerError;
                await response.WriteStringAsync("伺服器錯誤，請稍後再試");
                return response;
            }
        }

        [Function("ConfirmLinePayPayment")]
        public async Task<HttpResponseData> ConfirmPaymentAsync(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "linepay/confirm")] HttpRequestData req)
        {
            var response = req.CreateResponse();

            try
            {
                var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
                var transactionId = query["transactionId"];
                var amount = query["amount"];

                if (string.IsNullOrEmpty(transactionId) || string.IsNullOrEmpty(amount))
                {
                    response.StatusCode = HttpStatusCode.BadRequest;
                    await response.WriteStringAsync("缺少 transactionId 或 amount");
                    return response;
                }

                var result = await _linePayService.ConfirmPaymentAsync(transactionId, int.Parse(amount));

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

                var result = await _linePayService.GetPaymentDetailsAsync(transactionId, orderId);

                response.StatusCode = HttpStatusCode.OK;
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

        [Function("RefundLinePayPayment")]
        public async Task<HttpResponseData> RefundPaymentAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "linepay/refund")] HttpRequestData req)
        {
            var response = req.CreateResponse();

            try
            {
                var body = await req.ReadAsStringAsync();
                var refundRequest = JsonSerializer.Deserialize<LinePayRefundRequest>(body, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (refundRequest == null || string.IsNullOrEmpty(refundRequest.transactionId))
                {
                    response.StatusCode = HttpStatusCode.BadRequest;
                    await response.WriteStringAsync("缺少 transactionId");
                    return response;
                }

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
