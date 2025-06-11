using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class EcpayInvoiceService
{
    private readonly HttpClient _client;
    private const string MerchantID = "2000132";
    private const string HashKey = "ejCk326UnaZWKisg";
    private const string HashIV = "q9jcZX8Ib9LM8wYk";
    private const string ApiUrl = "https://einvoice-stage.ecpay.com.tw/B2CInvoice/Issue";

    public EcpayInvoiceService(HttpClient client)
    {
        _client = client;
    }

    public async Task<string> IssueInvoiceAsync(InvoiceRequest invoiceData)
    {
        var timestamp = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(8)).ToUnixTimeSeconds();

        var dataJson = JsonSerializer.Serialize(invoiceData);
        var urlEncodedData = Uri.EscapeDataString(dataJson); // ✅ URL encode
        var encryptedData = AESEncrypt(urlEncodedData, HashKey, HashIV); // ✅ Base64 加密結果

        var requestObj = new
        {
            MerchantID,
            RqHeader = new { Timestamp = timestamp },
            Data = encryptedData
        };

        var requestJson = JsonSerializer.Serialize(requestObj);
        var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync(ApiUrl, content);
        var result = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"綠界 API 失敗：{response.StatusCode}，內容：{result}");
        }

        return result;
    }

    /// <summary>
    /// ✅ 正確的 AES/CBC/PKCS7 加密，回傳 Base64 字串
    /// </summary>
    private static string AESEncrypt(string plainText, string key, string iv)
    {
        using var aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(key);
        aes.IV = Encoding.UTF8.GetBytes(iv);
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        using var encryptor = aes.CreateEncryptor();
        var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        return Convert.ToBase64String(encryptedBytes); // ✅ 回傳 Base64，而非 Hex
    }

    /// <summary>
    /// ✅ 解密 Base64 格式字串
    /// </summary>
    public static string AESDecrypt(string encryptedBase64, string key, string iv)
    {
        var encryptedBytes = Convert.FromBase64String(encryptedBase64); // ✅ 解 Base64

        using var aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(key);
        aes.IV = Encoding.UTF8.GetBytes(iv);
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor();
        var decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);

        return Encoding.UTF8.GetString(decryptedBytes);
    }
}
