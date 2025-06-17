using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OrderFoodPos.Models.Payments;

namespace OrderFoodPos.Services
{
    public class JkoPayService
    {
        private readonly HttpClient _httpClient;

        private const string MerchantID = "99999999";
        private const string MerchantKey = "2AA6B9B6F9C64247ABB2677B6AF2C896";
        private const string SystemName = "OrderFoodPos"; // 改為你的系統方名稱

        private static string PaymentUrl => $"https://uat-pos.jkopay.app/Test/Payment";
       

        public JkoPayService(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task<string> RequestPaymentAsync(JkoPayRequest request)
        {
            PrepareRequest(request);
            return await PostAsync(PaymentUrl, request);
        }

        
        private void PrepareRequest(JkoPayRequest request)
        {
            request.MerchantID = MerchantID;
            request.SendTime = DateTime.Now.ToString("yyyyMMddHHmmss");
            request.PosTradeTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");

            // 保證所有欄位都填入空字串
            request.Extra1 ??= "";
            request.Extra2 ??= "";
            request.Extra3 ??= "";
            request.GatewayTradeNo ??= "";
            request.Remark ??= "";

            request.Sign = GenerateSignature(request);
        }

        private async Task<string> PostAsync(string url, JkoPayRequest request)
        {
            var jsonBody = JsonConvert.SerializeObject(request);
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);
            var result = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"[街口API失敗] {response.StatusCode}: {result}");
            }

            return result;
        }

        private string GenerateSignature(JkoPayRequest request)
        {
            // 依照 API 文件順序產生簽章
            var sorted = new SortedDictionary<string, object>
            {
                { "CardToken", request.CardToken ?? "" },
                { "Extra1", request.Extra1 },
                { "Extra2", request.Extra2 },
                { "Extra3", request.Extra3 },
                { "GatewayTradeNo", request.GatewayTradeNo },
                { "MerchantID", request.MerchantID },
                { "MerchantTradeNo", request.MerchantTradeNo },
                { "PosID", request.PosID },
                { "PosTradeTime", request.PosTradeTime },
                { "Remark", request.Remark },
                { "SendTime", request.SendTime },
                { "StoreID", request.StoreID },
                { "StoreName", request.StoreName },
                { "TradeAmount", request.TradeAmount },
                { "UnRedeem", request.UnRedeem }
            };

            string json = JsonConvert.SerializeObject(sorted, Formatting.None);
            string toHash = json + MerchantKey;

            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(toHash));
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower(); // 回傳小寫
        }
    }
}
