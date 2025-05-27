using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OrderFoodPos;
using System.Data;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using OrderFoodPos.Repositories;
using OrderFoodPos.Services;
using OrderFoodPos.Repositories.Member;
using OrderFoodPos.Services.Member;
using OrderFoodPos.Repositories.Personnel;
using OrderFoodPos.Repositories.Store;
using OrderFoodPos.Services.Menu;
using OrderFoodPos.Repositories.Menu;
using OrderFoodPos.Services.Store;
using OrderFoodPos.Repositories.Charges;
using OrderFoodPos.Services.Charges;
using OrderFoodPos.Controllers;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();



//  注入 SqlConnectionFactory
builder.Services.AddSingleton<SqlConnectionFactory>();

//  注入 IDbConnection（讓所有 Repository 用同一條連線）
builder.Services.AddTransient<IDbConnection>(provider =>
{
	var factory = provider.GetRequiredService<SqlConnectionFactory>();
	return factory.CreateConnection();
});

//  Azure Blob Storage 服務
builder.Services.AddSingleton(x =>
{
	var config = x.GetRequiredService<IConfiguration>();
	var connStr = config["BlobConnectionString"];
	return new BlobServiceClient(connStr);
});

builder.Services.AddTransient<StoreAccountRepository>();
builder.Services.AddTransient<StoreAccountService>();
builder.Services.AddTransient<EmployeesRepository>();
builder.Services.AddTransient<EmployeesService>();
builder.Services.AddTransient<PersonnelRepository> ();
builder.Services.AddTransient<StoresRepository>();
builder.Services.AddTransient<CategoriesService>();
builder.Services.AddTransient<CategoriesRepository>();
builder.Services.AddTransient<ItemService>();
builder.Services.AddTransient<ItemRepository>();
builder.Services.AddTransient<TasteService>();
builder.Services.AddTransient<TasteRepository>();
builder.Services.AddTransient<CustomerService>();
builder.Services.AddTransient<CustomerRepository>();
builder.Services.AddTransient<TaxRepository>();
builder.Services.AddTransient<TaxService>();
builder.Services.AddTransient<ServiceFeeRepository>();
builder.Services.AddTransient<ServiceFeeService>();
builder.Services.AddTransient<LinePayService>();


// Application Insights isn't enabled by default. See https://aka.ms/AAt8mw4.
// builder.Services
//     .AddApplicationInsightsTelemetryWorkerService()
//     .ConfigureFunctionsApplicationInsights();

builder.Build().Run();
