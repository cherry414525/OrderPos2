using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OrderFoodPos.Models.Payments;

namespace OrderFoodPos.Services.Payments
{
    public class JkoPayService
    {
        private readonly HttpClient _httpClient;
        private const string ApiBaseUrl = "https://api.jkopay.com/v2"; // 假設 API 位置
        private const string MerchantId = "your_merchant_id";
        private const string ApiKey = "your_api_key";

        public JkoPayService()
        {
            _httpClient = new HttpClient();
        }

        public async Task<string> RequestPaymentAsync(JkoPayRequest request)
        {
            string apiPath = "/orders"; // 假設的 API 路徑

            var jsonBody = JsonConvert.SerializeObject(request);
            var signature = GenerateSignature(jsonBody);

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, ApiBaseUrl + apiPath);
            httpRequest.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            httpRequest.Headers.Add("X-Merchant-Id", MerchantId);
            httpRequest.Headers.Add("X-Signature", signature);

            var response = await _httpClient.SendAsync(httpRequest);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }

        private string GenerateSignature(string body)
        {
            // 根據街口支付規則產生簽章
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(ApiKey)))
            {
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(body));
                return Convert.ToBase64String(hash);
            }
        }
    }

}
