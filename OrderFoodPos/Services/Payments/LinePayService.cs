using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OrderFoodPos.Repositories.Payments;
using OrderFoodPos.Models.Payments;

namespace OrderFoodPos.Services
{
    public class LinePayService
    {
        private readonly HttpClient _httpClient;
        private readonly StoreLinePayRepository _storeLinePayRepo;

        // LINE Pay 沙盒 API 的基礎網址
        private const string ApiBaseUrl = "https://sandbox-api-pay.line.me";

        // 請替換為你自己的 Channel ID 和 Secret（可從 LINE Pay 商家後台取得）
        //private const string ChannelId = "2007487168";
        //private const string ChannelSecret = "3bc0177408f34129d91e9c08028ff1ac";

        public LinePayService(StoreLinePayRepository storeLinePayRepo)
        {
            _storeLinePayRepo = storeLinePayRepo;
            _httpClient = new HttpClient();

        }


        /// 發送付款請求
        public async Task<string> RequestPaymentAsync(object requestData)
        {
            string apiPath = "/v3/payments/request";
            return await SendRequestAsync("POST", apiPath, requestData, null);
        }


        /// 確認付款
        public async Task<string> ConfirmPaymentAsync(string transactionId, int amount)
        {
            string apiPath = $"/v3/payments/{transactionId}/confirm";
            var requestData = new
            {
                amount = amount,
                currency = "TWD" // 新台幣
            };
            return await SendRequestAsync("POST", apiPath, requestData, null);
        }


        /// 查詢付款詳情（可用交易 ID 或訂單 ID 查詢）
        public async Task<string> GetPaymentDetailsAsync(string[] transactionIds, string[] orderIds)
        {
            var queryParams = new List<string>();

            if (transactionIds != null)
                queryParams.AddRange(transactionIds.Select(id => $"transactionId={id}"));

            if (orderIds != null)
                queryParams.AddRange(orderIds.Select(id => $"orderId={id}"));

            string queryString = string.Join("&", queryParams);
            string apiPath = "/v3/payments";

            return await SendRequestAsync("GET", apiPath, null, queryString);
        }


        /// 進行退款，可指定部分金額退款（預設為全額退款）
        public async Task<string> RefundPaymentAsync(string transactionId, int refundAmount = 0)
        {
            string apiPath = $"/v3/payments/{transactionId}/refund";

            // 若有指定退款金額，則傳入 refundAmount，否則傳入空物件
            object requestData = refundAmount > 0 ? new { refundAmount } : new { };
            return await SendRequestAsync("POST", apiPath, requestData, null);
        }



        /// 發送 LINE Pay API 請求，並根據 HTTP 方法產生對應的簽章與標頭
      
        private async Task<string> SendRequestAsync(string method, string apiPath, object requestData, string queryString)
        {
            var storeConfig = await _storeLinePayRepo.GetStoreLinePayByStoreIdAsync("1");
            if (storeConfig == null)
                throw new Exception($"找不到 StoreID 的 LINE Pay 設定資料");

            string ChannelId = storeConfig.ChannelId;
            string ChannelSecret = storeConfig.ChannelSecret;


            string nonce = Guid.NewGuid().ToString(); // LINE 要求的亂數值（防止重播攻擊）

            string jsonBody = method == "POST" && requestData != null
                ? JsonConvert.SerializeObject(requestData)
                : string.Empty;

            string fullApiPath = apiPath;
            string messageToSign;

            if (method == "GET")
            {
                // GET: 加上查詢字串（但避免重複加問號）
                string queryPart = string.IsNullOrEmpty(queryString) ? "" : queryString;
                messageToSign = ChannelSecret + apiPath + queryPart + nonce;

                if (!string.IsNullOrEmpty(queryPart))
                {
                    fullApiPath += "?" + queryPart;
                }
            }
            else
            {
                // POST: 加 Body
                messageToSign = ChannelSecret + apiPath + jsonBody + nonce;
            }

            string signature = GenerateSignature(messageToSign,ChannelSecret);
            string requestUrl = ApiBaseUrl + fullApiPath;
            var requestMessage = new HttpRequestMessage(new HttpMethod(method), requestUrl);

            // 設定標頭（只設定這個 request 用的，不要清掉全域 DefaultRequestHeaders）
            requestMessage.Headers.Add("X-LINE-ChannelId", ChannelId);
            requestMessage.Headers.Add("X-LINE-Authorization-Nonce", nonce);
            requestMessage.Headers.Add("X-LINE-Authorization", signature);

            // POST 請求需加上內容
            if (method == "POST" && !string.IsNullOrEmpty(jsonBody))
            {
                requestMessage.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            }

            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)); // 加入逾時控制

                var response = await _httpClient.SendAsync(requestMessage, cts.Token);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"LINE Pay API 回應錯誤: {(int)response.StatusCode} {response.StatusCode}, 錯誤內容: {error}");
                }

                return await response.Content.ReadAsStringAsync();
            }
            catch (TaskCanceledException ex)
            {
                throw new TimeoutException("HTTP 請求逾時，請確認網路連線或 LINE Pay API 是否正常。", ex);
            }
            catch (HttpRequestException ex)
            {
                throw new Exception("HTTP 請求失敗，可能為網路問題或 API 錯誤。", ex);
            }
        }



        /// <summary>
        /// 使用 HMAC-SHA256 對輸入字串進行加密並轉成 Base64 簽章
        /// LINE Pay 需要開發者使用 ChannelSecret 作為金鑰，
        /// 將指定格式的字串（依照 HTTP 方法不同）產生 MAC 簽章，
        /// 並附加到 HTTP 標頭中的 X-LINE-Authorization。
        /// </summary>
        /// <param name="message">欲加密的原始訊息（由 API 路徑、內容、nonce 等組成）</param>
        /// <returns>經過加密並轉為 Base64 的簽章字串</returns>
        private string GenerateSignature(string message, string ChannelSecret)
        {
            if (string.IsNullOrEmpty(ChannelSecret))
                throw new InvalidOperationException("ChannelSecret 不可為空");

            if (message == null)
                throw new ArgumentNullException(nameof(message));

            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(ChannelSecret)))
            {
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
                return Convert.ToBase64String(hash);
            }
        }

    }
}
