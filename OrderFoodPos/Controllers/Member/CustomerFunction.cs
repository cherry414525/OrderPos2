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

	//�q��²�T�n�J
	[Function("CustomerFunction")]
	public async Task<HttpResponseData> Run(
	   [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "customer/login")] HttpRequestData req)
	{
		try
		{
			_logger.LogInformation("����Ȥ�n�J�ШD");

			//�ץ��G����Ū���y�A���]�m��m
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
				_logger.LogError(streamEx, $"Ū���ШD�y����: {streamEx.Message}");
				var streamErrorRes = req.CreateResponse(HttpStatusCode.BadRequest);
				await streamErrorRes.WriteStringAsync("�L�kŪ���ШD���e");
				return streamErrorRes;
			}

			_logger.LogInformation($"��l�ШD���e: '{requestBody}'");
			_logger.LogInformation($"�ШD���e����: {requestBody?.Length ?? 0}");

			// �O���ШD�Y�H��
			try
			{
				var contentType = req.Headers.Contains("Content-Type")
					? string.Join(", ", req.Headers.GetValues("Content-Type"))
					: "���]�m";
				_logger.LogInformation($"Content-Type: {contentType}");
			}
			catch
			{
				_logger.LogInformation("�L�k��� Content-Type");
			}

			// �ˬd�ШD��O�_����
			if (string.IsNullOrWhiteSpace(requestBody))
			{
				_logger.LogError("�ШD�鬰��");
				var emptyRes = req.CreateResponse(HttpStatusCode.BadRequest);
				await emptyRes.WriteStringAsync("�ШD�餣�ର�šA�нT�O�o�e���Ī� JSON �ƾ�");
				return emptyRes;
			}

			//�ץ��G��ʤϧǦC�� JSON
			FirebaseLoginDto? dto = null;
			try
			{
				var options = new JsonSerializerOptions
				{
					PropertyNameCaseInsensitive = true, // �����j�p�g
					AllowTrailingCommas = true,
					WriteIndented = true
				};

				dto = JsonSerializer.Deserialize<FirebaseLoginDto>(requestBody, options);
				_logger.LogInformation("JSON �ϧǦC�Ʀ��\");

				if (dto != null)
				{
					_logger.LogInformation($"DTO �Ա�:");
					_logger.LogInformation($"  - IdToken ����: {dto.IdToken?.Length ?? 0}");
					_logger.LogInformation($"  - IP: '{dto.Ip ?? "���]�m"}'");
					_logger.LogInformation($"  - UserAgent: '{dto.UserAgent ?? "���]�m"}'");
				}
			}
			catch (JsonException jsonEx)
			{
				_logger.LogError(jsonEx, $"JSON �ϧǦC�ƥ���: {jsonEx.Message}");
				_logger.LogError($"�L�Ī� JSON ���e: {requestBody}");
				var jsonErrorRes = req.CreateResponse(HttpStatusCode.BadRequest);
				await jsonErrorRes.WriteStringAsync($"JSON �榡���~: {jsonEx.Message}�C���ˬd JSON �y�k�O�_���T�C");
				return jsonErrorRes;
			}

			// ���� DTO
			if (dto == null)
			{
				_logger.LogError("DTO �ϧǦC�Ƶ��G�� null");
				var nullRes = req.CreateResponse(HttpStatusCode.BadRequest);
				await nullRes.WriteStringAsync("�L�k�ѪR�ШD�ƾ�");
				return nullRes;
			}

			if (string.IsNullOrEmpty(dto.IdToken))
			{
				_logger.LogError("IdToken �ʥ��ά���");
				var errorRes = req.CreateResponse(HttpStatusCode.BadRequest);
				await errorRes.WriteStringAsync("�ʤ� IdToken �� IdToken ����");
				return errorRes;
			}

			// �եΪA��
			try
			{
				_logger.LogInformation("�}�l�եεn�J�A��");
				var token = await _service.LoginOrCreateAsync(dto);

				if (string.IsNullOrEmpty(token))
				{
					_logger.LogError("�A�Ȫ�^�� token ����");
					var emptyTokenRes = req.CreateResponse(HttpStatusCode.InternalServerError);
					await emptyTokenRes.WriteStringAsync("�n�J���\���L�k�ͦ� token");
					return emptyTokenRes;
				}

				var res = req.CreateResponse(HttpStatusCode.OK);
				res.Headers.Add("Content-Type", "application/json");

				var responseData = new { token };
				var responseJson = JsonSerializer.Serialize(responseData);
				await res.WriteStringAsync(responseJson);

				_logger.LogInformation($"�n�J���\�A��^ token (����: {token.Length})");
				return res;
			}
			catch (Exception serviceEx)
			{
				_logger.LogError(serviceEx, $"�n�J�A�ȿ��~: {serviceEx.Message}");
				_logger.LogError($"�A�ȿ��~���: {serviceEx.StackTrace}");
				var serviceErrorRes = req.CreateResponse(HttpStatusCode.InternalServerError);
				await serviceErrorRes.WriteStringAsync($"�n�J���ѡG{serviceEx.Message}");
				return serviceErrorRes;
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, $"���w�������~: {ex.Message}");
			_logger.LogError($"���~���: {ex.StackTrace}");
			var unexpectedErrorRes = req.CreateResponse(HttpStatusCode.InternalServerError);
			await unexpectedErrorRes.WriteStringAsync($"���A���������~: {ex.Message}");
			return unexpectedErrorRes;
		}
	}



	//Line�n�J
	[Function("LineLogin")]
	public async Task<HttpResponseData> LineLogin(
	[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "customer/line-login")] HttpRequestData req)
	{
		var dto = await JsonSerializer.DeserializeAsync<LineCodeLoginDto>(req.Body);

		if (dto == null || string.IsNullOrEmpty(dto.Code) || string.IsNullOrEmpty(dto.RedirectUri))
		{
			var errorRes = req.CreateResponse(HttpStatusCode.BadRequest);
			await errorRes.WriteStringAsync("�ʤ� Code �� RedirectUri");
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
			_logger.LogError(ex, "LINE �n�J����");
			var res = req.CreateResponse(HttpStatusCode.InternalServerError);
			await res.WriteStringAsync("�n�J���ѡG" + ex.Message);
			return res;
		}
	}






}