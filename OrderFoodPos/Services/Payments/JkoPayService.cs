using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OrderFoodPos.Models.Payments;
using System.Text;
using OrderFoodPos.Repositories.Payments;
using OrderFoodPos.Repositories.Orders;
using Azure.Core;

namespace OrderFoodPos.Services
{
    public class JkoPayService
    {
        private readonly HttpClient _httpClient;
        private readonly StoreJkoPayRepository _storeJkoRepo;

        public JkoPayService(StoreJkoPayRepository storeJkoRepo, HttpClient httpClient)
        {
            _storeJkoRepo = storeJkoRepo;
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }
        //private const string MerchantID = "99999999";
        //private const string MerchantKey = "2AA6B9B6F9C64247ABB2677B6AF2C896";
        //private const string SystemName = "OrderFoodPos"; // 改為你的系統方名稱


        //街口付款
        public async Task<string> RequestPaymentAsync(JkoPayRequest request)
        {
            var (merchantId, merchantKey) = await SetStoreConfigAsync(request.StoreID);
            request.MerchantID = merchantId;
            var MerchantKey = merchantKey;
            //request.MerchantID = MerchantID;
            request.SendTime = DateTime.Now.ToString("yyyyMMddHHmmss");
            request.PosTradeTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");

            request.Extra1 ??= "";
            request.Extra2 ??= "";
            request.Extra3 ??= "";
            request.GatewayTradeNo ??= "";
            request.Remark ??= "";

            request.Sign = GenerateSignatureFromObject(new SortedDictionary<string, object>
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
        }, MerchantKey);

            var PaymentUrl ="https://uat-pos.jkopay.app/Test/Payment";
            return await PostAsync(PaymentUrl, request);
        }

        //街口取消付款
        public async Task<string> CancelPaymentAsync(JkoPayCancelRequest request)
        {
            var (merchantId, merchantKey) = await SetStoreConfigAsync(request.StoreID);
            request.MerchantID = merchantId;
            var MerchantKey = merchantKey;
            //request.MerchantID = MerchantID;
            request.SendTime = DateTime.Now.ToString("yyyyMMddHHmmss");
            request.PosTradeTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");

            request.Extra1 ??= "";
            request.Extra2 ??= "";
            request.Extra3 ??= "";
            request.GatewayTradeNo ??= "";
            request.Remark ??= "";

            request.Sign = GenerateSignatureFromObject(new SortedDictionary<string, object>
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
        }, MerchantKey);
            var CancelUrl = "https://uat-pos.jkopay.app/Test/Cancel";
            return await PostAsync(CancelUrl, request);
        }

        //街口退款
        public async Task<string> RefundPaymentAsync(JkoPayRefundRequest request)
        {
            var (merchantId, merchantKey) = await SetStoreConfigAsync(request.StoreID);
            request.MerchantID = merchantId;
            var MerchantKey = merchantKey;
            //request.MerchantID = MerchantID;
            request.SendTime = DateTime.Now.ToString("yyyyMMddHHmmss");
            request.PosTradeTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");

            request.Extra1 ??= "";
            request.Extra2 ??= "";
            request.Extra3 ??= "";
            request.GatewayTradeNo ??= "";
            request.Remark ??= "";

            request.Sign = GenerateSignatureFromObject(new SortedDictionary<string, object>
                {
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
                    { "TradeNo", request.TradeNo }
                }, MerchantKey);

            var refundUrl = "https://uat-pos.jkopay.app/Test/Refund";
            return await PostAsync(refundUrl, request);
        }

        //街口查詢
        public async Task<string> InquiryPaymentAsync(JkoPayInquiryRequest request)
        {
            
            var (merchantId, merchantKey) = await SetStoreConfigAsync(request.StoreID);
            request.MerchantID = merchantId;
            var MerchantKey=merchantKey;
            //request.MerchantID = MerchantID;

            request.SendTime = DateTime.Now.ToString("yyyyMMddHHmmss");
            request.PosTradeTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");

            request.Sign = GenerateSignatureFromObject(new SortedDictionary<string, object>
            {
                { "GatewayTradeNo", request.GatewayTradeNo ?? "" },
                { "InquiryType", request.InquiryType },
                { "MerchantID", request.MerchantID },
                { "MerchantTradeNo", request.MerchantTradeNo },
                { "PosID", request.PosID },
                { "PosTradeTime", request.PosTradeTime },
                { "SendTime", request.SendTime },
                { "StoreID", request.StoreID ?? "" }
            },MerchantKey);

            var inquiryUrl = "https://uat-pos.jkopay.app/Test/Inquiry";
            return await PostAsync(inquiryUrl, request);
        }
        
        
        //取得商家街口密碼
        private async Task<(string merchantId, string merchantKey)> SetStoreConfigAsync(String storeId)
        {
            var storeConfig = await _storeJkoRepo.GetStoreJkoPayByStoreIdAsync(storeId);
            if (storeConfig == null)
            {
                throw new Exception($"找不到 StoreID 為 {storeId} 的街口設定資料");
            }

            return (storeConfig.MerchantID, storeConfig.MerchantKey);
        }


        //傳送API給街口
        private async Task<string> PostAsync(string url, object request)
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

        /// 簽章設定
        private string GenerateSignatureFromObject(SortedDictionary<string, object> sortedFields,String MerchantKey)
        {
            string json = JsonConvert.SerializeObject(sortedFields, Formatting.None);
            
            string toHash = json + MerchantKey;

            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(toHash));

            string signature = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

            // 加入詳細 Log
            Console.WriteLine("=== 街口簽章產生紀錄 ===");
            Console.WriteLine("簽章 JSON 內容: " + json);
            Console.WriteLine("加密前原文: " + toHash);
            Console.WriteLine("產生簽章: " + signature);
            Console.WriteLine("==========================");

            return signature;
        }

    }
}
