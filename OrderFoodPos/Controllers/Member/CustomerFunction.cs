using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using OrderFoodPos.Models.Member;
using OrderFoodPos.Repositories.Member;
using OrderFoodPos.Services.Member;
using System.Net;
using System.Text.Json;


namespace OrderFoodPos.Controllers.Member;

public class CustomerFunction
{
    private readonly ILogger<CustomerFunction> _logger;
	private readonly CustomerService _service;

	public CustomerFunction(ILogger<CustomerFunction> logger, CustomerService service)
    {
        _logger = logger;
		_service = service;
	}

	//電話簡訊登入
	[Function("CustomerFunction")]
	public async Task<HttpResponseData> Run(
	   [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "customer/login")] HttpRequestData req)
	{
		try
		{
			_logger.LogInformation("收到客戶登入請求");

			//修正：直接讀取流，不設置位置
			string requestBody = "";
			try
			{
				using (var reader = new StreamReader(req.Body, leaveOpen: false))
				{
					requestBody = await reader.ReadToEndAsync();
				}
			}
			catch (Exception streamEx)
			{
				_logger.LogError(streamEx, $"讀取請求流失敗: {streamEx.Message}");
				var streamErrorRes = req.CreateResponse(HttpStatusCode.BadRequest);
				await streamErrorRes.WriteStringAsync("無法讀取請求內容");
				return streamErrorRes;
			}

			_logger.LogInformation($"原始請求內容: '{requestBody}'");
			_logger.LogInformation($"請求內容長度: {requestBody?.Length ?? 0}");

			// 記錄請求頭信息
			try
			{
				var contentType = req.Headers.Contains("Content-Type")
					? string.Join(", ", req.Headers.GetValues("Content-Type"))
					: "未設置";
				_logger.LogInformation($"Content-Type: {contentType}");
			}
			catch
			{
				_logger.LogInformation("無法獲取 Content-Type");
			}

			// 檢查請求體是否為空
			if (string.IsNullOrWhiteSpace(requestBody))
			{
				_logger.LogError("請求體為空");
				var emptyRes = req.CreateResponse(HttpStatusCode.BadRequest);
				await emptyRes.WriteStringAsync("請求體不能為空，請確保發送有效的 JSON 數據");
				return emptyRes;
			}

			//修正：手動反序列化 JSON
			FirebaseLoginDto? dto = null;
			try
			{
				var options = new JsonSerializerOptions
				{
					PropertyNameCaseInsensitive = true, // 忽略大小寫
					AllowTrailingCommas = true,
					WriteIndented = true
				};

				dto = JsonSerializer.Deserialize<FirebaseLoginDto>(requestBody, options);
				_logger.LogInformation("JSON 反序列化成功");

				if (dto != null)
				{
					_logger.LogInformation($"DTO 詳情:");
					_logger.LogInformation($"  - IdToken 長度: {dto.IdToken?.Length ?? 0}");
					_logger.LogInformation($"  - IP: '{dto.Ip ?? "未設置"}'");
					_logger.LogInformation($"  - UserAgent: '{dto.UserAgent ?? "未設置"}'");
				}
			}
			catch (JsonException jsonEx)
			{
				_logger.LogError(jsonEx, $"JSON 反序列化失敗: {jsonEx.Message}");
				_logger.LogError($"無效的 JSON 內容: {requestBody}");
				var jsonErrorRes = req.CreateResponse(HttpStatusCode.BadRequest);
				await jsonErrorRes.WriteStringAsync($"JSON 格式錯誤: {jsonEx.Message}。請檢查 JSON 語法是否正確。");
				return jsonErrorRes;
			}

			// 驗證 DTO
			if (dto == null)
			{
				_logger.LogError("DTO 反序列化結果為 null");
				var nullRes = req.CreateResponse(HttpStatusCode.BadRequest);
				await nullRes.WriteStringAsync("無法解析請求數據");
				return nullRes;
			}

			if (string.IsNullOrEmpty(dto.IdToken))
			{
				_logger.LogError("IdToken 缺失或為空");
				var errorRes = req.CreateResponse(HttpStatusCode.BadRequest);
				await errorRes.WriteStringAsync("缺少 IdToken 或 IdToken 為空");
				return errorRes;
			}

			// 調用服務
			try
			{
				_logger.LogInformation("開始調用登入服務");
				var token = await _service.LoginOrCreateAsync(dto);

				if (string.IsNullOrEmpty(token))
				{
					_logger.LogError("服務返回的 token 為空");
					var emptyTokenRes = req.CreateResponse(HttpStatusCode.InternalServerError);
					await emptyTokenRes.WriteStringAsync("登入成功但無法生成 token");
					return emptyTokenRes;
				}

				var res = req.CreateResponse(HttpStatusCode.OK);
				res.Headers.Add("Content-Type", "application/json");

				var responseData = new { token };
				var responseJson = JsonSerializer.Serialize(responseData);
				await res.WriteStringAsync(responseJson);

				_logger.LogInformation($"登入成功，返回 token (長度: {token.Length})");
				return res;
			}
			catch (Exception serviceEx)
			{
				_logger.LogError(serviceEx, $"登入服務錯誤: {serviceEx.Message}");
				_logger.LogError($"服務錯誤堆棧: {serviceEx.StackTrace}");
				var serviceErrorRes = req.CreateResponse(HttpStatusCode.InternalServerError);
				await serviceErrorRes.WriteStringAsync($"登入失敗：{serviceEx.Message}");
				return serviceErrorRes;
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, $"未預期的錯誤: {ex.Message}");
			_logger.LogError($"錯誤堆棧: {ex.StackTrace}");
			var unexpectedErrorRes = req.CreateResponse(HttpStatusCode.InternalServerError);
			await unexpectedErrorRes.WriteStringAsync($"伺服器內部錯誤: {ex.Message}");
			return unexpectedErrorRes;
		}
	}



	//Line登入
	[Function("LineLogin")]
	public async Task<HttpResponseData> LineLogin(
	[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "customer/line-login")] HttpRequestData req)
	{
		var dto = await JsonSerializer.DeserializeAsync<LineCodeLoginDto>(req.Body);

		if (dto == null || string.IsNullOrEmpty(dto.Code) || string.IsNullOrEmpty(dto.RedirectUri))
		{
			var errorRes = req.CreateResponse(HttpStatusCode.BadRequest);
			await errorRes.WriteStringAsync("缺少 Code 或 RedirectUri");
			return errorRes;
		}

		try
		{
			var token = await _service.LoginOrCreateWithLineCodeAsync(dto);
			var res = req.CreateResponse(HttpStatusCode.OK);
			res.Headers.Add("Content-Type", "application/json");

			var json = JsonSerializer.Serialize(new { token });
			await res.WriteStringAsync(json);
			return res;

		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "LINE 登入失敗");
			var res = req.CreateResponse(HttpStatusCode.InternalServerError);
			await res.WriteStringAsync("登入失敗：" + ex.Message);
			return res;
		}
	}






}