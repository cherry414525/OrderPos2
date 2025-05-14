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

			string requestBody = await new StreamReader(req.Body).ReadToEndAsync(); // �� ���T���o JSON �r��
			var body = JsonSerializer.Deserialize<ForgotPasswordRequest>(requestBody); // �� �� Json �r�갵�ϧǦC��

			var response = req.CreateResponse();

			if (body == null || string.IsNullOrWhiteSpace(body.Email))
			{
				response.StatusCode = HttpStatusCode.BadRequest;
				await response.WriteStringAsync("�п�J Email");
				return response;
			}

			bool success = await _resetPasswordService.ProcessForgotPasswordAsync(body.Email);

			if (!success)
			{
				response.StatusCode = HttpStatusCode.BadRequest;
				await response.WriteStringAsync("�䤣������� Email");
				return response;
			}

			response.StatusCode = HttpStatusCode.OK;
			await response.WriteStringAsync("���]�s���w�H�X");
			return response;
		}

		[Function("ValidateResetToken")]
		public async Task<HttpResponseData> ValidateResetToken(
			[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "reset-password/validate-token")] HttpRequestData req,
			FunctionContext context)
		{
			var logger = context.GetLogger("ResetPasswordFunction");

			string requestBody = await new StreamReader(req.Body).ReadToEndAsync(); // �� Ū�� body
			var body = JsonSerializer.Deserialize<ValidateTokenRequest>(requestBody); // �� �ন����

			var response = req.CreateResponse();

			if (body == null || string.IsNullOrWhiteSpace(body.Email) || string.IsNullOrWhiteSpace(body.Token))
			{
				response.StatusCode = HttpStatusCode.BadRequest;
				await response.WriteStringAsync("�ʤְѼ�");
				return response;
			}

			bool isValid = await _resetPasswordService.ValidateResetTokenAsync(body.Email, body.Token);

			if (isValid)
			{
				response.StatusCode = HttpStatusCode.OK;
				await response.WriteStringAsync("Token ����");
			}
			else
			{
				response.StatusCode = HttpStatusCode.BadRequest;
				await response.WriteStringAsync("Token �L�ĩΤw�L��");
			}

			return response;
		}


		[Function("ConfirmPasswordReset")]
		public async Task<HttpResponseData> ConfirmPasswordReset(
	[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "reset-password/confirm")] HttpRequestData req,
	FunctionContext context)
		{
			var logger = context.GetLogger("ResetPasswordFunction");

			string requestBody = await new StreamReader(req.Body).ReadToEndAsync(); // �� Ū�� JSON
			var body = JsonSerializer.Deserialize<ResetPasswordRequest>(requestBody); // �� �ѪR����

			var response = req.CreateResponse();

			if (body == null || string.IsNullOrWhiteSpace(body.Email) ||
				string.IsNullOrWhiteSpace(body.Token) || string.IsNullOrWhiteSpace(body.NewPassword))
			{
				response.StatusCode = HttpStatusCode.BadRequest;
				await response.WriteStringAsync("�ж�g�����T");
				return response;
			}

			bool success = await _resetPasswordService.ResetPasswordAsync(body.Email, body.Token, body.NewPassword);

			if (success)
			{
				response.StatusCode = HttpStatusCode.OK;
				await response.WriteStringAsync("�K�X�w���]���\");
			}
			else
			{
				response.StatusCode = HttpStatusCode.BadRequest;
				await response.WriteStringAsync("�K�X���]���ѡA�нT�{�s���αb�᪬�A");
			}

			return response;
		}

	}
}
