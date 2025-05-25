using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace OrderFoodPos.Controllers.Order;

public class OrderURLFunction
{
    private readonly ILogger<OrderURLFunction> _logger;

    public OrderURLFunction(ILogger<OrderURLFunction> logger)
    {
        _logger = logger;
    }

	[Function("GenerateOrderUrl")]
	public async Task<HttpResponseData> GenerateOrderUrl(
	[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "order/generate-link")] HttpRequestData req)
	{
		try
		{
			var body = await JsonSerializer.DeserializeAsync<JsonElement>(req.Body);
			int storeId = body.GetProperty("storeId").GetInt32();

			int hours = 2; // �w�] 2 �p��
			if (body.TryGetProperty("hours", out var hoursProp) && hoursProp.TryGetInt32(out var h))
			{
				hours = h;
			}

			var token = JwtHelper.GenerateOrderUrlToken(storeId, hours);
			var url = $"https://orderfoodpos.vercel.app/frontpage/order?token={token}";

			var response = req.CreateResponse(HttpStatusCode.OK);
			await response.WriteAsJsonAsync(new { url });
			return response;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "���ͭq�\���}����");
			var response = req.CreateResponse(HttpStatusCode.BadRequest);
			await response.WriteStringAsync("�ШD�榡���~�ίʤ� storeId");
			return response;
		}
	}


}