using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

public class EcpayInvoiceFunction
{
    private readonly EcpayInvoiceService _invoiceService;
    private readonly ILogger<EcpayInvoiceFunction> _logger;

    public EcpayInvoiceFunction(EcpayInvoiceService invoiceService, ILogger<EcpayInvoiceFunction> logger)
    {
        _invoiceService = invoiceService;
        _logger = logger;
    }

    [Function("IssueInvoice")]
    public async Task<HttpResponseData> IssueInvoiceAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "ecpayinvoice/issue")] HttpRequestData req)
    {
        var response = req.CreateResponse();

        try
        {
            var requestBody = await req.ReadAsStringAsync();
            _logger.LogInformation($"收到發票開立請求：{requestBody}");

            var invoiceData = JsonSerializer.Deserialize<InvoiceRequest>(requestBody);

            var apiResult = await _invoiceService.IssueInvoiceAsync(invoiceData);

            using var doc = JsonDocument.Parse(apiResult);
            var root = doc.RootElement;

            if (root.TryGetProperty("Data", out var dataElement))
            {
                var encryptedData = dataElement.GetString();
                var decryptedData = EcpayInvoiceService.AESDecrypt(encryptedData, "ejCk326UnaZWKisg", "q9jcZX8Ib9LM8wYk");
                var decodedData = System.Web.HttpUtility.UrlDecode(decryptedData);

                var responseJson = JsonSerializer.Serialize(JsonDocument.Parse(decodedData).RootElement, new JsonSerializerOptions { WriteIndented = true });

                response.StatusCode = HttpStatusCode.OK;
                response.Headers.Add("Content-Type", "application/json");
                await response.WriteStringAsync(responseJson);
            }
            else
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                await response.WriteStringAsync("回應中缺少 Data 欄位");
            }
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "發票開立失敗");
            response.StatusCode = HttpStatusCode.InternalServerError;
            await response.WriteStringAsync($"伺服器錯誤：{ex.Message}");
        }

        return response;
    }
}
