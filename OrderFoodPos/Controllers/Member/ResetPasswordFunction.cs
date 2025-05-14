using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using OrderFoodPos.Services.Member;
using static OrderFoodPos.Models.Member.ResetPassword;

namespace yochungmedical.Functions
{
	public class ResetPasswordFunction
	{
		private readonly ResetPasswordService _resetPasswordService;

		public ResetPasswordFunction(ResetPasswordService resetPasswordService)
		{
			_resetPasswordService = resetPasswordService;
		}

		[Function("RequestPasswordReset")]
		public async Task<HttpResponseData> RequestPasswordReset(
	[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "reset-password/request")] HttpRequestData req,
	FunctionContext context)
		{
			var logger = context.GetLogger("ResetPasswordFunction");

			string requestBody = await new StreamReader(req.Body).ReadToEndAsync(); // ← 正確取得 JSON 字串
			var body = JsonSerializer.Deserialize<ForgotPasswordRequest>(requestBody); // ← 用 Json 字串做反序列化

			var response = req.CreateResponse();

			if (body == null || string.IsNullOrWhiteSpace(body.Email))
			{
				response.StatusCode = HttpStatusCode.BadRequest;
				await response.WriteStringAsync("請輸入 Email");
				return response;
			}

			bool success = await _resetPasswordService.ProcessForgotPasswordAsync(body.Email);

			if (!success)
			{
				response.StatusCode = HttpStatusCode.BadRequest;
				await response.WriteStringAsync("找不到對應的 Email");
				return response;
			}

			response.StatusCode = HttpStatusCode.OK;
			await response.WriteStringAsync("重設連結已寄出");
			return response;
		}

		[Function("ValidateResetToken")]
		public async Task<HttpResponseData> ValidateResetToken(
			[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "reset-password/validate-token")] HttpRequestData req,
			FunctionContext context)
		{
			var logger = context.GetLogger("ResetPasswordFunction");

			string requestBody = await new StreamReader(req.Body).ReadToEndAsync(); // ← 讀取 body
			var body = JsonSerializer.Deserialize<ValidateTokenRequest>(requestBody); // ← 轉成物件

			var response = req.CreateResponse();

			if (body == null || string.IsNullOrWhiteSpace(body.Email) || string.IsNullOrWhiteSpace(body.Token))
			{
				response.StatusCode = HttpStatusCode.BadRequest;
				await response.WriteStringAsync("缺少參數");
				return response;
			}

			bool isValid = await _resetPasswordService.ValidateResetTokenAsync(body.Email, body.Token);

			if (isValid)
			{
				response.StatusCode = HttpStatusCode.OK;
				await response.WriteStringAsync("Token 有效");
			}
			else
			{
				response.StatusCode = HttpStatusCode.BadRequest;
				await response.WriteStringAsync("Token 無效或已過期");
			}

			return response;
		}


		[Function("ConfirmPasswordReset")]
		public async Task<HttpResponseData> ConfirmPasswordReset(
	[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "reset-password/confirm")] HttpRequestData req,
	FunctionContext context)
		{
			var logger = context.GetLogger("ResetPasswordFunction");

			string requestBody = await new StreamReader(req.Body).ReadToEndAsync(); // ← 讀取 JSON
			var body = JsonSerializer.Deserialize<ResetPasswordRequest>(requestBody); // ← 解析物件

			var response = req.CreateResponse();

			if (body == null || string.IsNullOrWhiteSpace(body.Email) ||
				string.IsNullOrWhiteSpace(body.Token) || string.IsNullOrWhiteSpace(body.NewPassword))
			{
				response.StatusCode = HttpStatusCode.BadRequest;
				await response.WriteStringAsync("請填寫完整資訊");
				return response;
			}

			bool success = await _resetPasswordService.ResetPasswordAsync(body.Email, body.Token, body.NewPassword);

			if (success)
			{
				response.StatusCode = HttpStatusCode.OK;
				await response.WriteStringAsync("密碼已重設成功");
			}
			else
			{
				response.StatusCode = HttpStatusCode.BadRequest;
				await response.WriteStringAsync("密碼重設失敗，請確認連結或帳戶狀態");
			}

			return response;
		}

	}
}
