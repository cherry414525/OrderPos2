using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace OrderFoodPos.Services
{
    public class LinePayService
    {
        private readonly HttpClient _httpClient;
        private const string ApiBaseUrl = "https://sandbox-api-pay.line.me"; // 沙盒環境
        private const string ChannelId = "2007487168";
        private const string ChannelSecret = "3bc0177408f34129d91e9c08028ff1ac";

        public LinePayService()
        {
            _httpClient = new HttpClient();
            // 不在此添加 Header，因為 nonce 和 signature 是每次動態生成的
        }

        public async Task<string> RequestPaymentAsync(object requestData)
        {
            string apiPath = "/v3/payments/request";
            string method = "POST";

            return await SendRequestAsync(method, apiPath, requestData);
        }

        private async Task<string> SendRequestAsync(string method, string apiPath, object requestData)
        {
            string nonce = Guid.NewGuid().ToString();
            string jsonBody = requestData != null ? JsonConvert.SerializeObject(requestData) : "";

            string signatureBase = ChannelSecret + apiPath + jsonBody + nonce;
            string signature = GenerateSignature(signatureBase);

            var requestUrl = ApiBaseUrl + apiPath;
            var requestMessage = new HttpRequestMessage(new HttpMethod(method), requestUrl);

            // Add required headers
            requestMessage.Headers.Add("X-LINE-ChannelId", ChannelId);
            requestMessage.Headers.Add("X-LINE-Authorization-Nonce", nonce);
            requestMessage.Headers.Add("X-LINE-Authorization", signature);

            if (method == "POST" && !string.IsNullOrEmpty(jsonBody))
            {
                requestMessage.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            }

            var response = await _httpClient.SendAsync(requestMessage);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }

        private string GenerateSignature(string content)
        {
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(ChannelSecret)))
            {
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(content));
                return Convert.ToBase64String(hash);
            }
        }

        public async Task<string> ConfirmPaymentAsync(string transactionId, int amount)
        {
            var apiPath = $"/v3/payments/{transactionId}/confirm";
            var requestData = new
            {
                amount = amount,
                currency = "TWD"
            };
            return await SendRequestAsync("POST", apiPath, requestData);
        }

        public async Task<string> GetPaymentDetailsAsync(string transactionId, string orderId)
        {
            var query = new List<string>();
            if (!string.IsNullOrEmpty(transactionId)) query.Add($"transactionId={transactionId}");
            if (!string.IsNullOrEmpty(orderId)) query.Add($"orderId={orderId}");

            string apiPath = "/v3/payments?" + string.Join("&", query);
            return await SendRequestAsync("GET", apiPath, null);
        }

        public async Task<string> RefundPaymentAsync(string transactionId, int refundAmount = 0)
        {
            string apiPath = $"/v3/payments/{transactionId}/refund";
            object requestData = refundAmount > 0 ? new { refundAmount = refundAmount } : new { };
            return await SendRequestAsync("POST", apiPath, requestData);
        }

    }
}
