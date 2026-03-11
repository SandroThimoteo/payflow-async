using Amazon.SQS;
using Microsoft.EntityFrameworkCore;
using Payment.Application.Abstractions;
using Payment.Infrastructure.Messaging;
using Payment.Infrastructure.Persistence;
using Payment.Infrastructure.Repositories;
using Payment.Processor;

var builder = Host.CreateApplicationBuilder(args);

// Banco de dados
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<PaymentDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// Repositório
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IUnitOfWork>(sp => (IUnitOfWork)sp.GetRequiredService<IPaymentRepository>());

// AWS SQS — usa credenciais do ambiente (AWS_ACCESS_KEY_ID, AWS_SECRET_ACCESS_KEY, AWS_REGION)
// Para desenvolvimento local com LocalStack, sobrescreva ServiceURL no appsettings
builder.Services.AddSingleton<IAmazonSQS>(_ =>
{
    var awsOptions = builder.Configuration.GetSection("Aws");
    var config = new AmazonSQSConfig();

    var serviceUrl = awsOptions["ServiceUrl"];
    if (!string.IsNullOrEmpty(serviceUrl))
        config.ServiceURL = serviceUrl; // LocalStack: http://localhost:4566

    return new AmazonSQSClient(config);
});

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
